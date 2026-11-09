namespace Fleet.Shared.Messaging.Outbox;

/// <summary>
/// Entity stored in the service's private database representing a message awaiting dispatch.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; // JSON-serialized event
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
}