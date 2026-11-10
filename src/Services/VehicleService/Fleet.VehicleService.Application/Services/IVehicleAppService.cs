using Fleet.Shared.Domain;
using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Domain.Models;

namespace Fleet.VehicleService.Application.Services;

public interface IVehicleAppService
{
    Task<Result<VehicleResponse>> CreateAsync(Guid tenantId, CreateVehicleRequest request, string actorId, CancellationToken ct);
    Task<Result<VehicleResponse>> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<Result<PagedResult<VehicleResponse>>> GetAllAsync(Guid tenantId, VehicleFilter filter, PaginationParams pagination, CancellationToken ct);
    Task<Result<VehicleResponse>> UpdateAsync(Guid id, Guid tenantId, UpdateVehicleRequest request, string actorId, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, Guid tenantId, string actorId, CancellationToken ct);
    Task<Result<VehicleResponse>> BindDeviceAsync(Guid id, Guid tenantId, BindDeviceRequest request, string actorId, CancellationToken ct);
    Task<Result> UnbindDeviceAsync(Guid id, Guid tenantId, string actorId, CancellationToken ct);
    Task<Result> UpdateOdometerAsync(Guid id, Guid tenantId, UpdateOdometerRequest request, string actorId, CancellationToken ct);
    Task<Result<MaintenanceRecordResponse>> AddMaintenanceRecordAsync(Guid id, Guid tenantId, CreateMaintenanceRecordRequest request, string actorId, CancellationToken ct);
    Task<Result<PagedResult<MaintenanceRecordResponse>>> GetMaintenanceHistoryAsync(Guid id, Guid tenantId, PaginationParams pagination, CancellationToken ct);
    Task<Result<VehicleSummaryResponse>> GetSummaryAsync(Guid id, Guid tenantId, CancellationToken ct);
}
