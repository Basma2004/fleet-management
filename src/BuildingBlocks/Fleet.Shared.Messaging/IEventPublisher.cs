namespace Fleet.Shared.Messaging;

/// <summary>
/// Abstraction for publishing domain integration events to the message broker.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
}