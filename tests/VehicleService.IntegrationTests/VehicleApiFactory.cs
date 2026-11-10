using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Fleet.VehicleService.Infrastructure.Persistence;
using Xunit;

namespace VehicleService.IntegrationTests;

public class VehicleApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("timescale/timescaledb-ha:pg16")
        .WithDatabase("fleet_vehicle_test")
        .WithUsername("fleet")
        .WithPassword("fleet_dev_pwd")
        .Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithUsername("fleet")
        .WithPassword("fleet_dev_pwd")
        .Build();

    public async Task InitializeAsync()
    {
        // 1. Boot up both containers in Docker
        await _dbContainer.StartAsync();
        await _rabbitContainer.StartAsync();

        // 2. Automatically apply all EF Core migrations to the test database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VehicleDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _rabbitContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Dynamically inject the randomized ports assigned by Docker
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["RabbitMQ:Host"] = _rabbitContainer.Hostname,
                ["RabbitMQ:Username"] = "fleet",
                ["RabbitMQ:Password"] = "fleet_dev_pwd"
            });
        });
    }
}
