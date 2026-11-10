using Fleet.Shared.Domain;
using Fleet.VehicleService.Domain.Enums;
using Fleet.VehicleService.Domain.Events;

namespace Fleet.VehicleService.Domain.Entities;

/// <summary>
/// The Vehicle Aggregate Root is the boundary of consistency for vehicle data,
/// device bindings, and maintenance history.
/// </summary>
public class Vehicle : AggregateRoot, IAuditableEntity
{
    public Guid TenantId { get; private set; }
    public string Vin { get; private set; } = string.Empty;
    public string PlateNumber { get; private set; } = string.Empty;
    public string? Make { get; private set; }
    public string? Model { get; private set; }
    public int? Year { get; private set; }
    public string? Type { get; private set; }
    public VehicleStatus Status { get; private set; }
    public decimal OdometerKm { get; private set; }
    public decimal EngineHours { get; private set; }
    public string? DeviceId { get; private set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    private readonly List<MaintenanceRecord> _maintenanceHistory = new();
    public IReadOnlyList<MaintenanceRecord> MaintenanceHistory => _maintenanceHistory.AsReadOnly();

    // Private parameterless constructor for EF Core
    private Vehicle() { }

    // Factory method to enforce valid initial creation
    public static Vehicle Create(
        Guid tenantId,
        string vin,
        string plateNumber,
        string? make,
        string? model,
        int? year,
        string? type)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Vin = vin.Trim().ToUpperInvariant(),
            PlateNumber = plateNumber.Trim().ToUpperInvariant(),
            Make = make?.Trim(),
            Model = model?.Trim(),
            Year = year,
            Type = type?.Trim(),
            Status = VehicleStatus.Active,
            OdometerKm = 0,
            EngineHours = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        vehicle.AddDomainEvent(new VehicleCreatedEvent(vehicle.Id, tenantId));
        return vehicle;
    }

    // Business Behavior: Update basic vehicle specifications
    public Result Update(string? make, string? model, int? year, string? type, string plateNumber)
    {
        if (Status == VehicleStatus.Retired)
            return Result.Failure("Cannot modify a retired vehicle.");

        if (string.IsNullOrWhiteSpace(plateNumber))
            return Result.Failure("Plate number is required.");

        Make = make?.Trim();
        Model = model?.Trim();
        Year = year;
        Type = type?.Trim();
        PlateNumber = plateNumber.Trim().ToUpperInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VehicleUpdatedEvent(Id, TenantId));
        return Result.Success();
    }

    // Business Behavior: Bind an IoT telematics device
    public Result BindDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return Result.Failure("Device ID cannot be empty.");

        if (!string.IsNullOrEmpty(DeviceId))
            return Result.Failure($"Vehicle is already bound to device '{DeviceId}'. Unbind it first.");

        DeviceId = deviceId.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VehicleDeviceBoundEvent(Id, DeviceId, TenantId));
        return Result.Success();
    }

    // Business Behavior: Unbind the active IoT device
    public Result UnbindDevice()
    {
        if (string.IsNullOrEmpty(DeviceId))
            return Result.Failure("No device is currently bound to this vehicle.");

        DeviceId = null;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VehicleUpdatedEvent(Id, TenantId));
        return Result.Success();
    }

    // Business Behavior: Update lifecycle status
    public Result UpdateStatus(VehicleStatus newStatus)
    {
        if (Status == VehicleStatus.Retired && newStatus != VehicleStatus.Retired)
            return Result.Failure("A retired vehicle cannot be reactivated.");

        if (Status == newStatus)
            return Result.Success();

        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VehicleUpdatedEvent(Id, TenantId));
        return Result.Success();
    }

    // Business Behavior: Monotonic odometer update
    public Result UpdateOdometer(decimal odometerKm, string source)
    {
        if (odometerKm < OdometerKm)
            return Result.Failure($"Odometer cannot decrease. Current: {OdometerKm} km, Reported: {odometerKm} km (Source: {source}).");

        OdometerKm = odometerKm;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VehicleUpdatedEvent(Id, TenantId));
        return Result.Success();
    }

    // Business Behavior: Append maintenance record
    public Result AddMaintenanceRecord(DateOnly serviceDate, string serviceType, decimal? cost, string? notes)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
            return Result.Failure("Service type is required.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (serviceDate > today)
            return Result.Failure("Service date cannot be in the future.");

        var record = MaintenanceRecord.Create(Id, serviceDate, serviceType.Trim(), cost, notes?.Trim());
        _maintenanceHistory.Add(record);
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VehicleMaintenanceRecordedEvent(Id, TenantId, serviceDate, serviceType.Trim()));
        return Result.Success();
    }
}