namespace Fleet.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published when a vehicle reports a new GPS position.
/// Consumed by Trip Service (mileage), Geofence evaluation, and Real-time UI.
/// </summary>
public record VehiclePositionUpdated(
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading,
    DateTimeOffset Timestamp
);