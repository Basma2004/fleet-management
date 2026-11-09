namespace Fleet.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Universal audit record published whenever an auditable action occurs in any service.
/// Consumed asynchronously by the Audit Service.
/// </summary>
public record AuditEvent(
    Guid EventId,
    string ActorId,
    string ActorType,     // "user" | "system" | "device"
    string Action,        // "create" | "update" | "delete" | "assign" | "status_change"
    string ResourceType,  // "vehicle" | "driver" | "trip" | "geofence"
    string ResourceId,
    Guid? TenantId,
    DateTimeOffset Timestamp,
    string? CorrelationId,
    Dictionary<string, object?>? Payload = null
);