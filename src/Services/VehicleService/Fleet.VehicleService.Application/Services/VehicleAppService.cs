using FluentValidation;
using Fleet.Shared.Contracts.IntegrationEvents;
using Fleet.Shared.Domain;
using Fleet.Shared.Messaging;
using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Application.Mappings;
using Fleet.VehicleService.Domain.Entities;
using Fleet.VehicleService.Domain.Enums;
using Fleet.VehicleService.Domain.Models;
using Fleet.VehicleService.Domain.Repositories;

namespace Fleet.VehicleService.Application.Services;

public class VehicleAppService : IVehicleAppService
{
    private readonly IVehicleRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IValidator<CreateVehicleRequest> _createValidator;
    private readonly IValidator<UpdateVehicleRequest> _updateValidator;
    private readonly IValidator<BindDeviceRequest> _bindValidator;
    private readonly IValidator<UpdateOdometerRequest> _odometerValidator;
    private readonly IValidator<CreateMaintenanceRecordRequest> _maintenanceValidator;

    public VehicleAppService(
        IVehicleRepository repository,
        IEventPublisher eventPublisher,
        IValidator<CreateVehicleRequest> createValidator,
        IValidator<UpdateVehicleRequest> updateValidator,
        IValidator<BindDeviceRequest> bindValidator,
        IValidator<UpdateOdometerRequest> odometerValidator,
        IValidator<CreateMaintenanceRecordRequest> maintenanceValidator)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _bindValidator = bindValidator;
        _odometerValidator = odometerValidator;
        _maintenanceValidator = maintenanceValidator;
    }

    public async Task<Result<VehicleResponse>> CreateAsync(
        Guid tenantId,
        CreateVehicleRequest request,
        string actorId,
        CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<VehicleResponse>(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var isVinUnique = await _repository.IsVinUniqueAsync(request.Vin, tenantId, null, ct);
        if (!isVinUnique)
            return Result.Failure<VehicleResponse>($"A vehicle with VIN '{request.Vin}' already exists for this tenant.");

        var isPlateUnique = await _repository.IsPlateUniqueAsync(request.PlateNumber, tenantId, null, ct);
        if (!isPlateUnique)
            return Result.Failure<VehicleResponse>($"A vehicle with plate number '{request.PlateNumber}' already exists for this tenant.");

        var vehicle = Vehicle.Create(
            tenantId,
            request.Vin,
            request.PlateNumber,
            request.Make,
            request.Model,
            request.Year,
            request.Type
        );

        vehicle.CreatedBy = actorId;
        vehicle.UpdatedBy = actorId;

        await _repository.AddAsync(vehicle, ct);

        // Publish Universal Audit Event
        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "create",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null
        ), ct);

        return Result.Success(vehicle.ToResponse());
    }

    public async Task<Result<VehicleResponse>> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure<VehicleResponse>("Vehicle not found.");

        return Result.Success(vehicle.ToResponse());
    }

    public async Task<Result<PagedResult<VehicleResponse>>> GetAllAsync(
        Guid tenantId,
        VehicleFilter filter,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var pagedVehicles = await _repository.GetAllAsync(tenantId, filter, pagination, ct);
        var dtos = pagedVehicles.Items.Select(v => v.ToResponse()).ToList();

        var result = new PagedResult<VehicleResponse>(
            dtos,
            pagedVehicles.TotalCount,
            pagedVehicles.Page,
            pagedVehicles.PageSize
        );

        return Result.Success(result);
    }

    public async Task<Result<VehicleResponse>> UpdateAsync(
        Guid id,
        Guid tenantId,
        UpdateVehicleRequest request,
        string actorId,
        CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<VehicleResponse>(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure<VehicleResponse>("Vehicle not found.");

        var isPlateUnique = await _repository.IsPlateUniqueAsync(request.PlateNumber, tenantId, vehicle.Id, ct);
        if (!isPlateUnique)
            return Result.Failure<VehicleResponse>($"A vehicle with plate number '{request.PlateNumber}' already exists for this tenant.");

        var updateResult = vehicle.Update(request.Make, request.Model, request.Year, request.Type, request.PlateNumber);
        if (!updateResult.IsSuccess)
            return Result.Failure<VehicleResponse>(updateResult.Error!);

        vehicle.UpdatedBy = actorId;
        await _repository.UpdateAsync(vehicle, ct);

        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "update",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null
        ), ct);

        return Result.Success(vehicle.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid id, Guid tenantId, string actorId, CancellationToken ct)
    {
        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure("Vehicle not found.");

        // Soft-delete rule: change status to Retired
        var statusResult = vehicle.UpdateStatus(VehicleStatus.Retired);
        if (!statusResult.IsSuccess)
            return statusResult;

        vehicle.UpdatedBy = actorId;
        await _repository.UpdateAsync(vehicle, ct);

        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "delete",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null
        ), ct);

        return Result.Success();
    }

    public async Task<Result<VehicleResponse>> BindDeviceAsync(
        Guid id,
        Guid tenantId,
        BindDeviceRequest request,
        string actorId,
        CancellationToken ct)
    {
        var validation = await _bindValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<VehicleResponse>(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure<VehicleResponse>("Vehicle not found.");

        var isDeviceBoundElsewhere = await _repository.IsDeviceBoundAsync(request.DeviceId, vehicle.Id, ct);
        if (isDeviceBoundElsewhere)
            return Result.Failure<VehicleResponse>($"Device '{request.DeviceId}' is already bound to another vehicle.");

        var bindResult = vehicle.BindDevice(request.DeviceId);
        if (!bindResult.IsSuccess)
            return Result.Failure<VehicleResponse>(bindResult.Error!);

        vehicle.UpdatedBy = actorId;
        await _repository.UpdateAsync(vehicle, ct);

        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "bind_device",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null,
            Payload: new Dictionary<string, object?> { ["deviceId"] = request.DeviceId }
        ), ct);

        return Result.Success(vehicle.ToResponse());
    }

    public async Task<Result> UnbindDeviceAsync(Guid id, Guid tenantId, string actorId, CancellationToken ct)
    {
        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure("Vehicle not found.");

        var unbindResult = vehicle.UnbindDevice();
        if (!unbindResult.IsSuccess)
            return unbindResult;

        vehicle.UpdatedBy = actorId;
        await _repository.UpdateAsync(vehicle, ct);

        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "unbind_device",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null
        ), ct);

        return Result.Success();
    }

    public async Task<Result> UpdateOdometerAsync(
        Guid id,
        Guid tenantId,
        UpdateOdometerRequest request,
        string actorId,
        CancellationToken ct)
    {
        var validation = await _odometerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure("Vehicle not found.");

        var odoResult = vehicle.UpdateOdometer(request.OdometerKm, request.Source);
        if (!odoResult.IsSuccess)
            return odoResult;

        vehicle.UpdatedBy = actorId;
        await _repository.UpdateAsync(vehicle, ct);

        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "update_odometer",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null,
            Payload: new Dictionary<string, object?>
            {
                ["odometerKm"] = request.OdometerKm,
                ["source"] = request.Source
            }
        ), ct);

        return Result.Success();
    }

    public async Task<Result<MaintenanceRecordResponse>> AddMaintenanceRecordAsync(
        Guid id,
        Guid tenantId,
        CreateMaintenanceRecordRequest request,
        string actorId,
        CancellationToken ct)
    {
        var validation = await _maintenanceValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure<MaintenanceRecordResponse>(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure<MaintenanceRecordResponse>("Vehicle not found.");

        var recordResult = vehicle.AddMaintenanceRecord(
            request.ServiceDate,
            request.ServiceType,
            request.Cost,
            request.Notes
        );

        if (!recordResult.IsSuccess)
            return Result.Failure<MaintenanceRecordResponse>(recordResult.Error!);

        vehicle.UpdatedBy = actorId;
        await _repository.UpdateAsync(vehicle, ct);

        var createdRecord = vehicle.MaintenanceHistory[^1];

        // 1. Universal Audit Event
        await _eventPublisher.PublishAsync(new AuditEvent(
            EventId: Guid.NewGuid(),
            ActorId: actorId,
            ActorType: "user",
            Action: "add_maintenance_record",
            ResourceType: "vehicle",
            ResourceId: vehicle.Id.ToString(),
            TenantId: tenantId,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: null
        ), ct);

        // 2. Integration Event for Predictive Maintenance / Downstream services
        await _eventPublisher.PublishAsync(new VehicleMaintenanceRecorded(
            VehicleId: vehicle.Id,
            ServiceDate: request.ServiceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            ServiceType: request.ServiceType,
            OdometerKm: (double)vehicle.OdometerKm
        ), ct);

        return Result.Success(createdRecord.ToResponse());
    }

    public async Task<Result<PagedResult<MaintenanceRecordResponse>>> GetMaintenanceHistoryAsync(
        Guid id,
        Guid tenantId,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure<PagedResult<MaintenanceRecordResponse>>("Vehicle not found.");

        var query = vehicle.MaintenanceHistory
            .OrderByDescending(m => m.ServiceDate)
            .ThenByDescending(m => m.CreatedAt);

        var totalCount = query.Count();
        var items = query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(m => m.ToResponse())
            .ToList();

        var pagedResult = new PagedResult<MaintenanceRecordResponse>(
            items,
            totalCount,
            pagination.Page,
            pagination.PageSize
        );

        return Result.Success(pagedResult);
    }

    public async Task<Result<VehicleSummaryResponse>> GetSummaryAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct)
    {
        var vehicle = await _repository.GetByIdAsync(id, tenantId, ct);
        if (vehicle is null)
            return Result.Failure<VehicleSummaryResponse>("Vehicle not found.");

        // For MVP, GPS coordinates and assigned driver name are nullable (integrated in Phase 5 & 8)
        return Result.Success(vehicle.ToSummaryResponse(
            lastKnownLat: null,
            lastKnownLon: null,
            assignedDriverName: null
        ));
    }
}