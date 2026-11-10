namespace Fleet.VehicleService.Application.DTOs;

public record CreateVehicleRequest(
    string Vin,
    string PlateNumber,
    string? Make,
    string? Model,
    int? Year,
    string? Type
);

public record UpdateVehicleRequest(
    string? Make,
    string? Model,
    int? Year,
    string? Type,
    string PlateNumber
);

public record BindDeviceRequest(
    string DeviceId
);

public record UpdateOdometerRequest(
    decimal OdometerKm,
    string Source // "manual" or "telemetry"
);

public record CreateMaintenanceRecordRequest(
    DateOnly ServiceDate,
    string ServiceType,
    decimal? Cost,
    string? Notes
);