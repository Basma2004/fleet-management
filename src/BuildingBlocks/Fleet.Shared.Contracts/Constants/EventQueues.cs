namespace Fleet.Shared.Contracts.Constants;

/// <summary>
/// Centralized names for RabbitMQ queues and routing keys.
/// Avoids magic strings scattered across publisher and consumer services.
/// </summary>
public static class EventQueues
{
    public const string AuditEvents = "fleet.audit.events";
    public const string VehiclePositionUpdated = "fleet.vehicle.position.updated";
    public const string DriverStatusChanged = "fleet.driver.status.changed";
    public const string TripCompleted = "fleet.trip.completed";
    public const string VehicleMaintenance = "fleet.vehicle.maintenance";
    public const string GeofenceAlerts = "fleet.geofence.alerts";
}