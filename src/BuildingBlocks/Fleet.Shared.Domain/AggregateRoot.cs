using System.Collections.Generic;

namespace Fleet.Shared.Domain;

/// <summary>
/// An Aggregate Root is the primary entry point to an entity cluster.
/// It maintains invariants and collects domain events during lifecycle changes
/// to be dispatched when changes are committed to the database.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
