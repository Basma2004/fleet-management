using Fleet.Shared.Domain;
using Fleet.VehicleService.Domain.Entities;
using Fleet.VehicleService.Domain.Models;

namespace Fleet.VehicleService.Domain.Repositories;

/// <summary>
/// Domain contract defining data persistence requirements for vehicles.
/// Implemented by the Infrastructure layer via EF Core and Dapper.
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<Vehicle?> GetByVinAsync(string vin, Guid tenantId, CancellationToken ct);
    Task<Vehicle?> GetByDeviceIdAsync(string deviceId, CancellationToken ct);
    Task<PagedResult<Vehicle>> GetAllAsync(Guid tenantId, VehicleFilter filter, PaginationParams pagination, CancellationToken ct);
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    Task UpdateAsync(Vehicle vehicle, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<bool> IsVinUniqueAsync(string vin, Guid tenantId, Guid? excludeId, CancellationToken ct);
    Task<bool> IsPlateUniqueAsync(string plate, Guid tenantId, Guid? excludeId, CancellationToken ct);
    Task<bool> IsDeviceBoundAsync(string deviceId, Guid? excludeVehicleId, CancellationToken ct);
}