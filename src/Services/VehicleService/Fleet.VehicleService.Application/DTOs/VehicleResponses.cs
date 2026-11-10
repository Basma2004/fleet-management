using Fleet.VehicleService.Domain.Enums;

namespace Fleet.VehicleService.Application.DTOs;

public record VehicleResponse(
    Guid Id,
    Guid TenantId,
    string Vin,
    string PlateNumber,
    string? Make,
    string? Model,
    int? Year,
    string? Type,
    VehicleStatus Status,
    decimal OdometerKm,
    decimal EngineHours,
    string? DeviceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record VehicleSummaryResponse(
    Guid Id,
    string Vin,
    string PlateNumber,
    VehicleStatus Status,
    decimal OdometerKm,
    string? DeviceId,
    double? LastKnownLat,
    double? LastKnownLon,
    string? AssignedDriverName
);

public record MaintenanceRecordResponse(
    Guid Id,
    Guid VehicleId,
    DateOnly ServiceDate,
    string ServiceType,
    decimal? Cost,
    string? Notes,
    DateTimeOffset CreatedAt
);