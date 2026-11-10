using Fleet.VehicleService.Domain.Enums;

namespace Fleet.VehicleService.Domain.Models;

public record VehicleFilter(
    VehicleStatus? Status = null,
    string? Type = null,
    string? Make = null
);