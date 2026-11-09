using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fleet.Shared.Messaging.Outbox;

/// <summary>
/// Background worker that periodically polls the outbox store and publishes messages to RabbitMQ.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(3);

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "An error occurred while processing the outbox queue.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxStore = scope.ServiceProvider.GetService<IOutboxStore>();

        // If the service hosting this worker hasn't registered an outbox store, skip gracefully
        if (outboxStore is null)
            return;

        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var messages = await outboxStore.GetUnprocessedMessagesAsync(batchSize: 20, ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    _logger.LogWarning("Unknown event type '{EventType}' for outbox message {MessageId}", message.EventType, message.Id);
                    await outboxStore.MarkAsFailedAsync(message.Id, $"Cannot resolve type {message.EventType}", ct);
                    continue;
                }

                var deserialized = JsonSerializer.Deserialize(message.Payload, eventType);
                if (deserialized is not null)
                {
                    await publishEndpoint.Publish(deserialized, eventType, ct);
                    await outboxStore.MarkAsProcessedAsync(message.Id, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch outbox message {MessageId}", message.Id);
                await outboxStore.MarkAsFailedAsync(message.Id, ex.Message, ct);
            }
        }
    }
}