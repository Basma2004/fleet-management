using Fleet.Shared.Domain;

namespace Fleet.VehicleService.Domain.Events;

public record VehicleUpdatedEvent(
    Guid VehicleId,
    Guid TenantId,
    Guid EventId = default,
    DateTimeOffset OccurredAt = default) : IDomainEvent
{
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;
    public DateTimeOffset OccurredAt { get; init; } = OccurredAt == default ? DateTimeOffset.UtcNow : OccurredAt;
    public string EventType => nameof(VehicleUpdatedEvent);
}