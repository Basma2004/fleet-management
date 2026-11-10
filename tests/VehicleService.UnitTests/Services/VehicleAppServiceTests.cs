using Moq;
using FluentAssertions;
using Fleet.Shared.Contracts.IntegrationEvents;
using Fleet.Shared.Messaging;
using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Application.Services;
using Fleet.VehicleService.Application.Validators;
using Fleet.VehicleService.Domain.Entities;
using Fleet.VehicleService.Domain.Repositories;
using Xunit;

namespace VehicleService.UnitTests.Services;

public class VehicleAppServiceTests
{
    private readonly Mock<IVehicleRepository> _repositoryMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly VehicleAppService _sut; // System Under Test
    private readonly Guid _tenantId = Guid.NewGuid();

    public VehicleAppServiceTests()
    {
        _sut = new VehicleAppService(
            _repositoryMock.Object,
            _eventPublisherMock.Object,
            new CreateVehicleRequestValidator(),
            new UpdateVehicleRequestValidator(),
            new BindDeviceRequestValidator(),
            new UpdateOdometerRequestValidator(),
            new CreateMaintenanceRecordRequestValidator()
        );
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValidAndVinIsUnique_ShouldPersistAndPublishAuditEvent()
    {
        // Arrange
        var request = new CreateVehicleRequest("1HGCR2F83HA123456", "ABC-1234", "Ford", "F-150", 2021, "Truck");

        _repositoryMock
            .Setup(r => r.IsVinUniqueAsync(request.Vin, _tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.IsPlateUniqueAsync(request.PlateNumber, _tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(_tenantId, request, "user-123", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Vin.Should().Be(request.Vin);

        // Verify repository saved the entity
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Vehicle>(v => v.Vin == request.Vin), It.IsAny<CancellationToken>()), Times.Once);

        // Verify universal audit event was dispatched to the message bus
        _eventPublisherMock.Verify(p => p.PublishAsync(
            It.Is<AuditEvent>(e => e.Action == "create" && e.ActorId == "user-123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenVinAlreadyExists_ShouldReturnFailureWithoutCallingAddAsync()
    {
        // Arrange
        var request = new CreateVehicleRequest("1HGCR2F83HA123456", "ABC-1234", "Ford", "F-150", 2021, "Truck");

        _repositoryMock
            .Setup(r => r.IsVinUniqueAsync(request.Vin, _tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Duplicate VIN found!

        // Act
        var result = await _sut.CreateAsync(_tenantId, request, "user-123", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists for this tenant");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BindDeviceAsync_WhenDeviceAlreadyBoundElsewhere_ShouldReturnFailure()
    {
        // Arrange
        var vehicle = Vehicle.Create(_tenantId, "1HGCR2F83HA123456", "ABC-1234", "Ford", "F-150", 2021, "Truck");
        var request = new BindDeviceRequest("IOT-COLLISION");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(vehicle.Id, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _repositoryMock
            .Setup(r => r.IsDeviceBoundAsync("IOT-COLLISION", vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Already bound to another vehicle!

        // Act
        var result = await _sut.BindDeviceAsync(vehicle.Id, _tenantId, request, "user-123", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already bound to another vehicle");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
