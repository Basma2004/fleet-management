namespace Fleet.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published when a vehicle enters or exits a predefined spatial boundary.
/// </summary>
public record GeofenceEvent(
    Guid VehicleId,
    Guid GeofenceId,
    string EventType, // "entered" | "exited"
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);