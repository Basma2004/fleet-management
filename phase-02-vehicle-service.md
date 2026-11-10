# Phase 2 — Vehicle Management Service

> **Duration:** 1.5 weeks
> **Prerequisites:** Phase 1 (Foundation) complete
> **Depends on:** Phase 1
> **Unlocks:** Phase 5 (Trip Service), Phase 7 (Predictive Maintenance)
> **PRD Requirements:** VEH-01 through VEH-06

---

## 1. Objective

Implement the Vehicle / Asset Management Service with full CRUD operations, IoT device binding, maintenance history (append-only), and event publishing. This service follows Clean Architecture and serves as the reference implementation for all other services.

---

## 2. Project Setup

### 2.1 — Create the 4 Projects

Create the following projects and add them to `FleetManagement.sln` under a `Services/VehicleService` solution folder:

| Project | Template | Path |
|---------|----------|------|
| `Fleet.VehicleService.Domain` | `classlib` | `src/Services/VehicleService/Fleet.VehicleService.Domain/` |
| `Fleet.VehicleService.Application` | `classlib` | `src/Services/VehicleService/Fleet.VehicleService.Application/` |
| `Fleet.VehicleService.Infrastructure` | `classlib` | `src/Services/VehicleService/Fleet.VehicleService.Infrastructure/` |
| `Fleet.VehicleService.Api` | `webapi` | `src/Services/VehicleService/Fleet.VehicleService.Api/` |

### 2.2 — Project References

```
Domain        → Fleet.Shared.Domain
Application   → Domain, Fleet.Shared.Contracts
Infrastructure→ Application, Fleet.Shared.Messaging, Fleet.Shared.Observability
Api           → Application, Infrastructure, Fleet.Shared.Auth, Fleet.Shared.Observability
```

### 2.3 — NuGet Packages

| Project | Packages |
|---------|----------|
| Application | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` |
| Infrastructure | `Npgsql.EntityFrameworkCore.PostgreSQL`, `Dapper`, `Microsoft.EntityFrameworkCore.Design` (dev) |
| Api | `Swashbuckle.AspNetCore` |

---

## 3. Domain Layer (`Fleet.VehicleService.Domain`)

### 3.1 — Entities

**`Vehicle.cs`** — Aggregate root:
```csharp
public class Vehicle : AggregateRoot, IAuditableEntity
{
    public Guid TenantId { get; private set; }
    public string Vin { get; private set; }
    public string PlateNumber { get; private set; }
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
    public IReadOnlyList<MaintenanceRecord> MaintenanceHistory
        => _maintenanceHistory.AsReadOnly();

    // Factory method
    public static Vehicle Create(Guid tenantId, string vin, string plateNumber,
        string? make, string? model, int? year, string? type)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Vin = vin,
            PlateNumber = plateNumber,
            Make = make,
            Model = model,
            Year = year,
            Type = type,
            Status = VehicleStatus.Active,
            OdometerKm = 0,
            EngineHours = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        vehicle.AddDomainEvent(new VehicleCreatedEvent(vehicle.Id, tenantId));
        return vehicle;
    }

    // Behavior methods
    public Result BindDevice(string deviceId) { /* ... */ }
    public Result UnbindDevice() { /* ... */ }
    public Result UpdateStatus(VehicleStatus newStatus) { /* ... */ }
    public Result UpdateOdometer(decimal odometerKm, string source) { /* ... */ }
    public Result AddMaintenanceRecord(DateOnly serviceDate, string serviceType,
        decimal? cost, string? notes) { /* ... */ }
    public Result Update(string? make, string? model, int? year,
        string? type, string plateNumber) { /* ... */ }
}
```

**`MaintenanceRecord.cs`** — Entity (not aggregate root):
```csharp
public class MaintenanceRecord : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public string ServiceType { get; private set; }
    public decimal? Cost { get; private set; }
    public string? Notes { get; private set; }
    // No update methods — append-only
}
```

### 3.2 — Enums

**`VehicleStatus.cs`**:
```csharp
public enum VehicleStatus { Active, Maintenance, Retired }
```

### 3.3 — Domain Events

- `VehicleCreatedEvent(Guid VehicleId, Guid TenantId)`
- `VehicleUpdatedEvent(Guid VehicleId, Guid TenantId)`
- `VehicleDeviceBoundEvent(Guid VehicleId, string DeviceId, Guid TenantId)`
- `VehicleMaintenanceRecordedEvent(Guid VehicleId, Guid TenantId, DateOnly ServiceDate, string ServiceType)`

All implement `IDomainEvent`.

### 3.4 — Repository Interface

```csharp
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
```

**`VehicleFilter`** — record with optional `Status`, `Type`, `Make` fields.

---

## 4. Application Layer (`Fleet.VehicleService.Application`)

### 4.1 — DTOs

**Requests:**
- `CreateVehicleRequest` — vin, plateNumber, make, model, year, type
- `UpdateVehicleRequest` — make, model, year, type, plateNumber
- `BindDeviceRequest` — deviceId
- `UpdateOdometerRequest` — odometerKm, source ("manual"/"telemetry")
- `CreateMaintenanceRecordRequest` — serviceDate, serviceType, cost, notes

**Responses:**
- `VehicleResponse` — all vehicle fields + createdAt, updatedAt
- `VehicleSummaryResponse` — id, vin, plateNumber, status, odometerKm, deviceId, lastKnownLat, lastKnownLon, assignedDriverName
- `MaintenanceRecordResponse` — all maintenance record fields

### 4.2 — Validators (FluentValidation)

- `CreateVehicleRequestValidator`:
  - VIN: required, 17 characters, alphanumeric
  - PlateNumber: required, max 20 chars
  - Year: if provided, between 1900 and current year + 1
- `UpdateVehicleRequestValidator`: similar rules
- `BindDeviceRequestValidator`: deviceId required, non-empty
- `UpdateOdometerRequestValidator`: odometerKm > 0, source in ("manual", "telemetry")
- `CreateMaintenanceRecordRequestValidator`: serviceDate required and not in future, serviceType required

### 4.3 — Service Interface & Implementation

**`IVehicleService.cs`:**
```csharp
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
```

**`VehicleAppService.cs`** — Implementation:
- Injects `IVehicleRepository`, `IEventPublisher`, `IValidator<T>`.
- Each write operation:
  1. Validates input with FluentValidation.
  2. Calls domain methods (which add domain events).
  3. Persists via repository.
  4. Publishes integration events (audit event + domain-specific events) via `IEventPublisher`.
  5. Returns `Result<T>`.
- The `GetSummary` method calls the domain repo for vehicle data. For MVP, `lastKnownLat`/`lastKnownLon` and `assignedDriverName` are nullable and will be populated via HTTP calls to GPS/Driver services (added in Phase 5/8). For now, return null for those fields.

### 4.4 — Mapping

Use a static `VehicleMapper` class (or extension methods) to map between domain entities and DTOs. Avoid AutoMapper to reduce magic — explicit mapping is preferred.

---

## 5. Infrastructure Layer (`Fleet.VehicleService.Infrastructure`)

### 5.1 — EF Core DbContext

**`VehicleDbContext.cs`:**
```csharp
public class VehicleDbContext : DbContext
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VehicleDbContext).Assembly);
    }
}
```

### 5.2 — Entity Configurations

**`VehicleConfiguration.cs`** (implements `IEntityTypeConfiguration<Vehicle>`):
- Table name: `vehicles`
- `Id` as primary key
- `Vin` + `TenantId` unique index
- `PlateNumber` + `TenantId` unique index
- `DeviceId` unique index (nullable)
- `Status` stored as `text` (string conversion from enum)
- `OdometerKm` precision(12, 2)
- `EngineHours` precision(10, 2)
- Navigation to `MaintenanceHistory` with `HasMany` / `WithOne`

**`MaintenanceRecordConfiguration.cs`**:
- Table name: `vehicle_maintenance_history`
- `VehicleId` foreign key
- Index on `VehicleId` + `ServiceDate`

**`OutboxMessageConfiguration.cs`**:
- Table name: `outbox_messages`
- Index on `ProcessedAt` (filtered where null)

### 5.3 — Repository Implementation

**`VehicleRepository.cs`** — Implements `IVehicleRepository` using `VehicleDbContext`:
- Uses EF Core for writes and simple reads.
- Uses Dapper for optimized list queries (via `GetAllAsync` with filtering).
- `GetAllAsync` uses raw SQL with Dapper for pagination performance:
  ```sql
  SELECT * FROM vehicles
  WHERE tenant_id = @TenantId
    AND (@Status IS NULL OR status = @Status)
    AND (@Type IS NULL OR type = @Type)
  ORDER BY created_at DESC
  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
  ```

### 5.4 — EF Core Migration

Create the initial migration:
```
dotnet ef migrations add InitialCreate -p src/Services/VehicleService/Fleet.VehicleService.Infrastructure -s src/Services/VehicleService/Fleet.VehicleService.Api
```

### 5.5 — Outbox Store Implementation

**`VehicleOutboxStore.cs`** — Implements `IOutboxStore` using `VehicleDbContext` to insert outbox messages in the same transaction as domain changes.

### 5.6 — Dependency Injection Registration

**`DependencyInjection.cs`**:
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddVehicleInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VehicleDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IOutboxStore, VehicleOutboxStore>();
        return services;
    }
}
```

---

## 6. API Layer (`Fleet.VehicleService.Api`)

### 6.1 — Controllers

**`VehiclesController.cs`** — `[Route("api/v1/vehicles")]`:

| Method | Route | PRD ID | Description |
|--------|-------|--------|-------------|
| `POST` | `/` | VEH-01 | Create vehicle |
| `GET` | `/` | VEH-01 | List vehicles (paginated, filtered) |
| `GET` | `/{id}` | VEH-01 | Get vehicle by ID |
| `PUT` | `/{id}` | VEH-01 | Update vehicle |
| `DELETE` | `/{id}` | VEH-01 | Soft delete (set status to Retired) |
| `POST` | `/{id}/device` | VEH-02 | Bind device to vehicle |
| `DELETE` | `/{id}/device` | VEH-02 | Unbind device |
| `PATCH` | `/{id}/odometer` | VEH-03 | Update odometer |
| `POST` | `/{id}/maintenance` | VEH-04 | Add maintenance record |
| `GET` | `/{id}/maintenance` | VEH-04 | Get maintenance history |
| `GET` | `/{id}/summary` | VEH-05 | Get vehicle summary |

**Implementation pattern for each action:**
```csharp
[HttpPost]
[ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
public async Task<IActionResult> Create(
    [FromBody] CreateVehicleRequest request,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    CancellationToken ct)
{
    var tenantId = _currentUser.TenantId;
    var result = await _vehicleService.CreateAsync(tenantId, request, _currentUser.UserId, ct);

    if (!result.IsSuccess)
        return Problem(result.Error, statusCode: /* map error to status code */);

    return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
}
```

### 6.2 — Error Handling Middleware

**`GlobalExceptionHandler.cs`** — Catches unhandled exceptions, logs them, and returns RFC 7807 problem details. Never expose stack traces in production.

### 6.3 — `Program.cs`

Follow the standard template from Phase 1, section 2.9. Add:
```csharp
builder.Services.AddVehicleInfrastructure(builder.Configuration);
builder.Services.AddScoped<IVehicleAppService, VehicleAppService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();
```

### 6.4 — `appsettings.json`

Use the standard template with `ConnectionStrings:DefaultConnection` pointing to `fleet_vehicle` database.

### 6.5 — Dockerfile

Use the standard template from Phase 1, replacing `{ServiceName}` with `VehicleService`.

### 6.6 — Add to Docker Compose

Add to `deploy/docker-compose.override.yml`:
```yaml
vehicle-service:
  build:
    context: ../
    dockerfile: src/Services/VehicleService/Fleet.VehicleService.Api/Dockerfile
  ports:
    - "5101:8080"
  environment:
    - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=fleet_vehicle;Username=fleet;Password=fleet_dev_pwd
    - RabbitMQ__Host=rabbitmq
  depends_on:
    postgres:
      condition: service_healthy
    rabbitmq:
      condition: service_started
```

---

## 7. Testing

### 7.1 — Unit Tests (`tests/VehicleService.UnitTests/`)

**Target:** Domain logic and Application service.

| Test class | Coverage target |
|------------|-----------------|
| `VehicleTests.cs` | `Vehicle.Create()` sets correct defaults; `BindDevice()` fails if already bound; `UpdateStatus()` validates transitions; `AddMaintenanceRecord()` adds event; domain events are emitted. |
| `VehicleAppServiceTests.cs` | Mock `IVehicleRepository` and `IEventPublisher`. Test create (happy path, duplicate VIN → 409), update, delete, bind device (already bound → 409), odometer update. |
| `CreateVehicleRequestValidatorTests.cs` | VIN length, required fields, year range. |

**NuGet packages:** `xUnit`, `FluentAssertions`, `Moq`, `FluentValidation.TestHelper`.

### 7.2 — Integration Tests (`tests/VehicleService.IntegrationTests/`)

**Target:** API endpoints against a real PostgreSQL database.

**Setup:**
- Use `Testcontainers` to spin up a PostgreSQL container with the `timescale/timescaledb-ha:pg16` image.
- Use `WebApplicationFactory<Program>` to host the API in-memory.
- Apply EF Core migrations before each test run.
- Use a RabbitMQ Testcontainer for messaging.

| Test class | Tests |
|------------|-------|
| `VehicleCrudTests.cs` | Create → Get → Update → Delete lifecycle; list with pagination; filter by status. |
| `DeviceBindingTests.cs` | Bind device → verify; bind same device to another vehicle → 409; unbind. |
| `MaintenanceHistoryTests.cs` | Add record → list records; verify append-only (no update endpoint). |
| `UniqueConstraintTests.cs` | Duplicate VIN → 409; duplicate plate → 409. |

---

## 8. Events Published by This Service

| Event | Queue/Topic | Trigger |
|-------|-------------|---------|
| `AuditEvent` | `fleet.audit.events` | Every create, update, delete, bind, unbind, maintenance record |
| `VehicleUpdatedEvent` | `fleet.vehicle.updated` | Vehicle updated |
| `VehicleDeviceBoundEvent` | `fleet.vehicle.device.bound` | Device bound to vehicle |
| `VehicleMaintenanceRecordedEvent` | `fleet.vehicle.maintenance.recorded` | Maintenance record added |

---

## 9. Acceptance Criteria (Phase 2 Complete)

- [ ] All VEH-01 through VEH-06 requirements implemented and tested.
- [ ] `POST /api/v1/vehicles` creates a vehicle; returns 201.
- [ ] `GET /api/v1/vehicles` returns paginated list filtered by tenant.
- [ ] `GET /api/v1/vehicles/{id}` returns vehicle; 404 if not found.
- [ ] `PUT /api/v1/vehicles/{id}` updates vehicle; 400 on validation failure.
- [ ] `DELETE /api/v1/vehicles/{id}` soft-deletes (status → Retired).
- [ ] `POST /api/v1/vehicles/{id}/device` binds device; 409 if already bound elsewhere.
- [ ] `PATCH /api/v1/vehicles/{id}/odometer` updates odometer with source flag.
- [ ] `POST /api/v1/vehicles/{id}/maintenance` appends record; no update/delete endpoints.
- [ ] `GET /api/v1/vehicles/{id}/maintenance` returns paginated history.
- [ ] `GET /api/v1/vehicles/{id}/summary` returns summary (GPS/driver fields nullable for now).
- [ ] VIN and plate uniqueness enforced per tenant (409 on conflict).
- [ ] Audit events published to RabbitMQ for every state change.
- [ ] Unit tests pass with ≥ 80% domain/application coverage.
- [ ] Integration tests pass against Testcontainers PostgreSQL.
- [ ] Swagger UI accessible at `/swagger`.
- [ ] Service starts in Docker Compose alongside infrastructure.
