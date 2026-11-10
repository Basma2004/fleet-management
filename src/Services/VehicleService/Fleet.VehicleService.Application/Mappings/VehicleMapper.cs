using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Domain.Entities;

namespace Fleet.VehicleService.Application.Mappings;

public static class VehicleMapper
{
    public static VehicleResponse ToResponse(this Vehicle vehicle)
    {
        return new VehicleResponse(
            vehicle.Id,
            vehicle.TenantId,
            vehicle.Vin,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.Type,
            vehicle.Status,
            vehicle.OdometerKm,
            vehicle.EngineHours,
            vehicle.DeviceId,
            vehicle.CreatedAt,
            vehicle.UpdatedAt
        );
    }

    public static VehicleSummaryResponse ToSummaryResponse(
        this Vehicle vehicle,
        double? lastKnownLat = null,
        double? lastKnownLon = null,
        string? assignedDriverName = null)
    {
        return new VehicleSummaryResponse(
            vehicle.Id,
            vehicle.Vin,
            vehicle.PlateNumber,
            vehicle.Status,
            vehicle.OdometerKm,
            vehicle.DeviceId,
            lastKnownLat,
            lastKnownLon,
            assignedDriverName
        );
    }

    public static MaintenanceRecordResponse ToResponse(this MaintenanceRecord record)
    {
        return new MaintenanceRecordResponse(
            record.Id,
            record.VehicleId,
            record.ServiceDate,
            record.ServiceType,
            record.Cost,
            record.Notes,
            record.CreatedAt
        );
    }
}