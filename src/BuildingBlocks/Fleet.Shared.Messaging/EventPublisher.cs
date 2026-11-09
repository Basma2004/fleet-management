using System.Diagnostics;
using MassTransit;

namespace Fleet.Shared.Messaging;

/// <summary>
/// Dispatches integration events via MassTransit IPublishEndpoint.
/// Automatically attaches distributed tracing / correlation IDs to outgoing messages.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        await _publishEndpoint.Publish(@event, context =>
        {
            // Propagate W3C distributed trace / activity ID across the message bus
            var activityId = Activity.Current?.Id;
            if (!string.IsNullOrEmpty(activityId))
            {
                context.Headers.Set("X-Correlation-ID", activityId);
            }
        }, ct);
    }
}