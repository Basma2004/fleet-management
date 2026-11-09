using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fleet.Shared.Observability.Extensions;

/// <summary>
/// Health check extensions separating Liveness (/health) and Readiness (/ready) probes.
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddFleetHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        // 1. PostgreSQL Health Check (if connection string is configured for this service)
        var dbConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(dbConnection))
        {
            healthChecks.AddNpgSql(dbConnection, name: "postgres", tags: ["ready"]);
        }

        // 2. RabbitMQ Health Check
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "fleet";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "fleet_dev_pwd";
        var rabbitUri = $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/";

        healthChecks.AddRabbitMQ(new Uri(rabbitUri), name: "rabbitmq", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapFleetHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe: Is the HTTP service alive and running?
        // Does NOT check downstream dependencies so network blips don't cause container restarts.
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness probe: Are backing dependencies (PostgreSQL, RabbitMQ) reachable?
        // Traffic is only routed to the service when this returns 200 OK.
        endpoints.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return endpoints;
    }
}