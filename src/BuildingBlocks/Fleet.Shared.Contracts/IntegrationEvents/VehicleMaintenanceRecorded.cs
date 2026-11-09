namespace Fleet.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published when maintenance or inspection is completed on a vehicle.
/// Consumed by Predictive Maintenance service to reset degradation models.
/// </summary>
public record VehicleMaintenanceRecorded(
    Guid VehicleId,
    DateTimeOffset ServiceDate,
    string ServiceType,
    double OdometerKm
);