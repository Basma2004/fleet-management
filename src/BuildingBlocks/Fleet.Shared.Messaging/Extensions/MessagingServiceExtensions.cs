using Fleet.Shared.Messaging.Outbox;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fleet.Shared.Messaging.Extensions;

/// <summary>
/// Service collection extension to register MassTransit, RabbitMQ transport, and the Outbox worker.
/// </summary>
public static class MessagingServiceExtensions
{
    public static IServiceCollection AddFleetMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(x =>
        {
            // Allow calling microservice to register its consumers
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"] ?? "localhost";
                var username = configuration["RabbitMQ:Username"] ?? "fleet";
                var password = configuration["RabbitMQ:Password"] ?? "fleet_dev_pwd";

                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Automatically configures endpoints for all registered consumers
                cfg.ConfigureEndpoints(context);
            });
        });

        // Register publisher abstraction and background outbox polling service
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}