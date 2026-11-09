namespace Fleet.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published when a vehicle finishes a trip.
/// Triggers maintenance mileage checks, driver timesheet updates, and audit logging.
/// </summary>
public record TripCompleted(
    Guid TripId,
    Guid DriverId,
    Guid VehicleId,
    double DistanceKm,
    double DurationMinutes,
    DateTimeOffset CompletedAt
);