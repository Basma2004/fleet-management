using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fleet.Shared.Messaging.Outbox;
using Fleet.VehicleService.Domain.Repositories;
using Fleet.VehicleService.Infrastructure.Persistence;
using Fleet.VehicleService.Infrastructure.Persistence.Outbox;
using Fleet.VehicleService.Infrastructure.Persistence.Repositories;

namespace Fleet.VehicleService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVehicleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<VehicleDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(VehicleDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IOutboxStore, VehicleOutboxStore>();

        return services;
    }
}