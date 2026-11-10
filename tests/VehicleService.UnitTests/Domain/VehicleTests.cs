using FluentAssertions;
using Fleet.VehicleService.Domain.Entities;
using Fleet.VehicleService.Domain.Enums;
using Fleet.VehicleService.Domain.Events;
using Xunit;

namespace VehicleService.UnitTests.Domain;

public class VehicleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectlyAndEmitCreatedEvent()
    {
        // Act
        var vehicle = Vehicle.Create(
            _tenantId,
            "1HGCR2F83HA000000",
            "ABC-1234",
            "Honda",
            "Accord",
            2023,
            "Sedan"
        );

        // Assert
        vehicle.Should().NotBeNull();
        vehicle.TenantId.Should().Be(_tenantId);
        vehicle.Vin.Should().Be("1HGCR2F83HA000000");
        vehicle.PlateNumber.Should().Be("ABC-1234");
        vehicle.Status.Should().Be(VehicleStatus.Active);
        vehicle.OdometerKm.Should().Be(0);
        vehicle.EngineHours.Should().Be(0);
        vehicle.DeviceId.Should().BeNull();
        vehicle.MaintenanceHistory.Should().BeEmpty();

        // Verify Domain Event was recorded
        vehicle.DomainEvents.Should().ContainSingle(e => e is VehicleCreatedEvent)
            .Which.Should().BeOfType<VehicleCreatedEvent>()
            .Which.VehicleId.Should().Be(vehicle.Id);
    }

    [Fact]
    public void BindDevice_WhenNoDeviceBound_ShouldSucceedAndEmitDeviceBoundEvent()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA000000", "ABC-1234", "Honda", "Accord", 2023, "Sedan");

        // Act
        var result = vehicle.BindDevice("IOT-DEV-99");

        // Assert
        result.IsSuccess.Should().BeTrue();
        vehicle.DeviceId.Should().Be("IOT-DEV-99");
        vehicle.DomainEvents.Should().Contain(e => e is VehicleDeviceBoundEvent);
    }

    [Fact]
    public void BindDevice_WhenDeviceAlreadyBound_ShouldFail()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA000000", "ABC-1234", "Honda", "Accord", 2023, "Sedan");
        vehicle.BindDevice("FIRST-DEVICE");

        // Act
        var result = vehicle.BindDevice("SECOND-DEVICE");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already bound to device 'FIRST-DEVICE'");
        vehicle.DeviceId.Should().Be("FIRST-DEVICE");
    }

    [Fact]
    public void UpdateOdometer_WhenIncreasing_ShouldUpdateOdometer()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA000000", "ABC-1234", "Honda", "Accord", 2023, "Sedan");

        // Act
        var result = vehicle.UpdateOdometer(1250.75m, "telemetry");

        // Assert
        result.IsSuccess.Should().BeTrue();
        vehicle.OdometerKm.Should().Be(1250.75m);
        vehicle.DomainEvents.Should().Contain(e => e is VehicleUpdatedEvent);
    }

    [Fact]
    public void UpdateOdometer_WhenDecreasing_ShouldFailInvariant()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA000000", "ABC-1234", "Honda", "Accord", 2023, "Sedan");
        vehicle.UpdateOdometer(5000m, "manual");

        // Act
        var result = vehicle.UpdateOdometer(4500m, "manual"); // Attempting to roll back odometer

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Odometer cannot decrease");
        vehicle.OdometerKm.Should().Be(5000m);
    }

    [Fact]
    public void AddMaintenanceRecord_WithValidData_ShouldAppendRecordAndEmitEvent()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA000000", "ABC-1234", "Honda", "Accord", 2023, "Sedan");
        var serviceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        // Act
        var result = vehicle.AddMaintenanceRecord(serviceDate, "Brake Pad Replacement", 350.00m, "Front and rear replaced");

        // Assert
        result.IsSuccess.Should().BeTrue();
        vehicle.MaintenanceHistory.Should().HaveCount(1);
        vehicle.MaintenanceHistory[0].ServiceType.Should().Be("Brake Pad Replacement");
        vehicle.MaintenanceHistory[0].Cost.Should().Be(350.00m);
        vehicle.DomainEvents.Should().Contain(e => e is VehicleMaintenanceRecordedEvent);
    }

    [Fact]
    public void AddMaintenanceRecord_WithFutureDate_ShouldFail()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA000000", "ABC-1234", "Honda", "Accord", 2023, "Sedan");
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        // Act
        var result = vehicle.AddMaintenanceRecord(futureDate, "Oil Change", 50m, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("future");
        vehicle.MaintenanceHistory.Should().BeEmpty();
    }
}
