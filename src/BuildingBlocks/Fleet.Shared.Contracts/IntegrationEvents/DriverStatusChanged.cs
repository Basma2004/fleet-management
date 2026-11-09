namespace Fleet.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published when a driver's availability or duty status changes.
/// </summary>
public record DriverStatusChanged(
    Guid DriverId,
    string PreviousStatus,
    string NewStatus,
    DateTimeOffset Timestamp
);