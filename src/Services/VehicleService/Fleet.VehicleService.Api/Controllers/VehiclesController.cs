using Microsoft.AspNetCore.Mvc;
using Fleet.Shared.Auth;
using Fleet.Shared.Domain;
using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Application.Services;
using Fleet.VehicleService.Domain.Enums;
using Fleet.VehicleService.Domain.Models;

namespace Fleet.VehicleService.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleAppService _vehicleService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(
        IVehicleAppService vehicleService,
        ICurrentUser currentUser,
        ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService;
        _currentUser = currentUser;
        _logger = logger;
    }

    // Resolves Tenant ID from authenticated claims or fallback X-Tenant-ID header (for local testing)
    private Guid TenantId
    {
        get
        {
            if (Guid.TryParse(_currentUser.TenantId, out var id))
                return id;

            if (Request.Headers.TryGetValue("X-Tenant-ID", out var headerVal) &&
                Guid.TryParse(headerVal.FirstOrDefault(), out var headerId))
                return headerId;

            // Default development tenant if neither is supplied
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
    }

    private string ActorId => _currentUser.UserId ?? "system";

    // VEH-01: Create Vehicle
    [HttpPost]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVehicleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await _vehicleService.CreateAsync(TenantId, request, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    // VEH-01: List Vehicles (Paginated & Filtered)
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] VehicleStatus? status,
        [FromQuery] string? type,
        [FromQuery] string? make,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new VehicleFilter(status, type, make);
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };

        var result = await _vehicleService.GetAllAsync(TenantId, filter, pagination, ct);
        return Ok(result.Value);
    }

    // VEH-01: Get Vehicle by ID
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.GetByIdAsync(id, TenantId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return Ok(result.Value);
    }

    // VEH-01: Update Vehicle Details
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken ct)
    {
        var result = await _vehicleService.UpdateAsync(id, TenantId, request, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return Ok(result.Value);
    }

    // VEH-01: Soft-Delete (Retire Vehicle)
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.DeleteAsync(id, TenantId, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return NoContent();
    }

    // VEH-02: Bind IoT Device
    [HttpPost("{id:guid}/device")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BindDevice(
        Guid id,
        [FromBody] BindDeviceRequest request,
        CancellationToken ct)
    {
        var result = await _vehicleService.BindDeviceAsync(id, TenantId, request, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return Ok(result.Value);
    }

    // VEH-02: Unbind IoT Device
    [HttpDelete("{id:guid}/device")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbindDevice(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.UnbindDeviceAsync(id, TenantId, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return NoContent();
    }

    // VEH-03: Update Odometer
    [HttpPatch("{id:guid}/odometer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOdometer(
        Guid id,
        [FromBody] UpdateOdometerRequest request,
        CancellationToken ct)
    {
        var result = await _vehicleService.UpdateOdometerAsync(id, TenantId, request, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return NoContent();
    }

    // VEH-04: Add Maintenance Record (Append-Only)
    [HttpPost("{id:guid}/maintenance")]
    [ProducesResponseType(typeof(MaintenanceRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMaintenanceRecord(
        Guid id,
        [FromBody] CreateMaintenanceRecordRequest request,
        CancellationToken ct)
    {
        var result = await _vehicleService.AddMaintenanceRecordAsync(id, TenantId, request, ActorId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // VEH-04: Get Maintenance History (Paginated)
    [HttpGet("{id:guid}/maintenance")]
    [ProducesResponseType(typeof(PagedResult<MaintenanceRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMaintenanceHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var result = await _vehicleService.GetMaintenanceHistoryAsync(id, TenantId, pagination, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return Ok(result.Value);
    }

    // VEH-05: Get Vehicle Summary
    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(VehicleSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.GetSummaryAsync(id, TenantId, ct);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.Error!);

        return Ok(result.Value);
    }

    // Helper to translate business Result errors to HTTP status codes
    private IActionResult MapErrorToResponse(string error)
    {
        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = error,
                Instance = HttpContext.Request.Path
            });
        }

        if (error.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("already bound", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict / Duplicate Resource",
                Detail = error,
                Instance = HttpContext.Request.Path
            });
        }

        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request / Validation Failure",
            Detail = error,
            Instance = HttpContext.Request.Path
        });
    }
}