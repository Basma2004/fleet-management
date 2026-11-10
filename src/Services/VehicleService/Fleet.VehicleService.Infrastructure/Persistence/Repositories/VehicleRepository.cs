using Dapper;
using Microsoft.EntityFrameworkCore;
using Fleet.Shared.Domain;
using Fleet.VehicleService.Domain.Entities;
using Fleet.VehicleService.Domain.Models;
using Fleet.VehicleService.Domain.Repositories;

namespace Fleet.VehicleService.Infrastructure.Persistence.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly VehicleDbContext _context;

    public VehicleRepository(VehicleDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _context.Vehicles
            .Include(v => v.MaintenanceHistory)
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct);
    }

    public async Task<Vehicle?> GetByVinAsync(string vin, Guid tenantId, CancellationToken ct)
    {
        var normalizedVin = vin.Trim().ToUpperInvariant();

        return await _context.Vehicles
            .Include(v => v.MaintenanceHistory)
            .FirstOrDefaultAsync(v => v.Vin == normalizedVin && v.TenantId == tenantId, ct);
    }

    public async Task<Vehicle?> GetByDeviceIdAsync(string deviceId, CancellationToken ct)
    {
        var normalizedDeviceId = deviceId.Trim();

        return await _context.Vehicles
            .Include(v => v.MaintenanceHistory)
            .FirstOrDefaultAsync(v => v.DeviceId == normalizedDeviceId, ct);
    }

    public async Task<PagedResult<Vehicle>> GetAllAsync(
        Guid tenantId,
        VehicleFilter filter,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var connection = _context.Database.GetDbConnection();

        // Parameterized dynamic SQL for Dapper high-speed reads
        var statusStr = filter.Status?.ToString();
        var offset = (pagination.Page - 1) * pagination.PageSize;

        const string countSql = @"
            SELECT COUNT(*) 
            FROM vehicles 
            WHERE tenant_id = @TenantId
              AND (@Status IS NULL OR status = @Status)
              AND (@Type IS NULL OR type = @Type)
              AND (@Make IS NULL OR make = @Make);";

        const string querySql = @"
            SELECT id 
            FROM vehicles 
            WHERE tenant_id = @TenantId
              AND (@Status IS NULL OR status = @Status)
              AND (@Type IS NULL OR type = @Type)
              AND (@Make IS NULL OR make = @Make)
            ORDER BY created_at DESC
            OFFSET @Offset LIMIT @PageSize;";

        var parameters = new
        {
            TenantId = tenantId,
            Status = statusStr,
            Type = filter.Type,
            Make = filter.Make,
            Offset = offset,
            PageSize = pagination.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var ids = (await connection.QueryAsync<Guid>(querySql, parameters)).ToList();

        if (ids.Count == 0)
        {
            return new PagedResult<Vehicle>(new List<Vehicle>(), totalCount, pagination.Page, pagination.PageSize);
        }

        // Materialize the matching entities with change tracking disabled for read performance
        var items = await _context.Vehicles
            .AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

        return new PagedResult<Vehicle>(items, totalCount, pagination.Page, pagination.PageSize);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct)
    {
        await _context.Vehicles.AddAsync(vehicle, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken ct)
    {
        var entry = _context.Entry(vehicle);
        if (entry.State == EntityState.Detached)
        {
            _context.Vehicles.Update(vehicle);
        }
        else
        {
            // When child entities with pre-generated GUIDs are added to an already-tracked aggregate,
            // explicitly tell EF Core to INSERT them rather than trying to UPDATE them.
            foreach (var record in vehicle.MaintenanceHistory)
            {
                var recordEntry = _context.Entry(record);
                if (recordEntry.State != EntityState.Unchanged)
                {
                    recordEntry.State = EntityState.Added;
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _context.Vehicles
            .AnyAsync(v => v.Id == id && v.TenantId == tenantId, ct);
    }

    public async Task<bool> IsVinUniqueAsync(string vin, Guid tenantId, Guid? excludeId, CancellationToken ct)
    {
        var normalizedVin = vin.Trim().ToUpperInvariant();
        return !await _context.Vehicles
            .AnyAsync(v => v.TenantId == tenantId && v.Vin == normalizedVin && (!excludeId.HasValue || v.Id != excludeId.Value), ct);
    }

    public async Task<bool> IsPlateUniqueAsync(string plate, Guid tenantId, Guid? excludeId, CancellationToken ct)
    {
        var normalizedPlate = plate.Trim().ToUpperInvariant();
        return !await _context.Vehicles
            .AnyAsync(v => v.TenantId == tenantId && v.PlateNumber == normalizedPlate && (!excludeId.HasValue || v.Id != excludeId.Value), ct);
    }

    public async Task<bool> IsDeviceBoundAsync(string deviceId, Guid? excludeVehicleId, CancellationToken ct)
    {
        var normalizedDeviceId = deviceId.Trim();
        return await _context.Vehicles
            .AnyAsync(v => v.DeviceId == normalizedDeviceId && (!excludeVehicleId.HasValue || v.Id != excludeVehicleId.Value), ct);
    }
}