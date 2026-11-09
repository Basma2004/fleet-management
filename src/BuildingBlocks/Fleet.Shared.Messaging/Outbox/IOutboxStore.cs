namespace Fleet.Shared.Messaging.Outbox;

/// <summary>
/// Storage interface implemented by each microservice's Infrastructure layer (using EF Core).
/// </summary>
public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 20, CancellationToken ct = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken ct = default);
    Task SaveAsync(OutboxMessage message, CancellationToken ct = default);
}