using Fleet.Shared.Auth;
using Fleet.Shared.Messaging.Extensions;
using Fleet.Shared.Observability.Extensions;
using Fleet.Shared.Observability.Middleware;
using Fleet.VehicleService.Api.Middleware;
using Fleet.VehicleService.Application.Services;
using Fleet.VehicleService.Application.Validators;
using Fleet.VehicleService.Infrastructure;
using Fleet.VehicleService.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Observability: Serilog & OpenTelemetry
builder.Host.UseFleetObservability("vehicle-service");

// 2. Controllers & Swagger Documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fleet Vehicle Service API",
        Version = "v1",
        Description = "Vehicle Asset Management, Telematics Binding, and Maintenance History Service"
    });
});

// 3. Exception Handling (RFC 7807)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. Infrastructure & Database
builder.Services.AddVehicleInfrastructure(builder.Configuration);

// 5. Messaging & Outbox Processor
builder.Services.AddFleetMessaging(builder.Configuration);

// 6. Application Layer Services & Validators
builder.Services.AddScoped<IVehicleAppService, VehicleAppService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVehicleRequestValidator>();

// 7. Multi-Tenant / User Context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// 8. Health Checks (Postgres & RabbitMQ)
builder.Services.AddFleetHealthChecks(builder.Configuration);

var app = builder.Build();


// Automatically apply pending EF Core migrations on container startup in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<VehicleDbContext>();
    await dbContext.Database.MigrateAsync();
}
// HTTP Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle Service API v1");
    });
}

// Exception Handling & Correlation ID Tracking
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRouting();

// Map Health & Readiness Probes (/health, /ready)
app.MapFleetHealthChecks();

// Map Controllers
app.MapControllers();

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }