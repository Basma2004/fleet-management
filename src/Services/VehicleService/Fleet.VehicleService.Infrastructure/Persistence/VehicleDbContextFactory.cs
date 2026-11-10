using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Fleet.VehicleService.Infrastructure.Persistence;

public class VehicleDbContextFactory : IDesignTimeDbContextFactory<VehicleDbContext>
{
    public VehicleDbContext CreateDbContext(string[] args)
    {
        // 1. Locate the Api directory where appsettings.json lives
        var apiPath = Path.Combine(Directory.GetCurrentDirectory(), "src/Services/VehicleService/Fleet.VehicleService.Api");
        if (!Directory.Exists(apiPath))
        {
            // Fallback if running terminal directly from the Api folder
            apiPath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 2. Read connection string from appsettings with fallback
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=fleet_vehicle;Username=fleet;Password=fleet_dev_pwd";

        var optionsBuilder = new DbContextOptionsBuilder<VehicleDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(VehicleDbContext).Assembly.FullName);
        });

        return new VehicleDbContext(optionsBuilder.Options);
    }
}