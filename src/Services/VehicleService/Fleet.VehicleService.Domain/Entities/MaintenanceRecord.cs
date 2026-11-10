using Fleet.Shared.Domain;

namespace Fleet.VehicleService.Domain.Entities;

/// <summary>
/// An immutable, append-only record representing maintenance performed on a vehicle.
/// Belongs to the Vehicle aggregate root and cannot be updated or deleted.
/// </summary>
public class MaintenanceRecord : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public string ServiceType { get; private set; } = string.Empty;
    public decimal? Cost { get; private set; }
    public string? Notes { get; private set; }

    // Private constructor for EF Core materialization
    private MaintenanceRecord() { }

    internal static MaintenanceRecord Create(
        Guid vehicleId,
        DateOnly serviceDate,
        string serviceType,
        decimal? cost,
        string? notes)
    {
        return new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            ServiceDate = serviceDate,
            ServiceType = serviceType,
            Cost = cost,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}