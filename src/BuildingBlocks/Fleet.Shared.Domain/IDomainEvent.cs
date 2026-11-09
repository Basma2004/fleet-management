namespace Fleet.Shared.Domain;

/// <summary>
/// Marker interface for events raised within the domain boundary.
/// Domain events capture something that happened in the domain that other parts
/// of the same domain (or other services via integration events) care about.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string EventType { get; }
}
