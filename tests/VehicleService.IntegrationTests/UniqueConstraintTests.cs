using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Fleet.VehicleService.Application.DTOs;
using Xunit;

namespace VehicleService.IntegrationTests;

public class UniqueConstraintTests : IClassFixture<VehicleApiFactory>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UniqueConstraintTests(VehicleApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenantId.ToString());
    }

    [Fact]
    public async Task CreateVehicle_WhenVinAlreadyExistsForTenant_ShouldReturn409Conflict()
    {
        var request1 = new CreateVehicleRequest("1HGCR2F83HA777777", "PLATE-1", "Honda", "Civic", 2020, "Sedan");
        var res1 = await _client.PostAsJsonAsync("/api/v1/vehicles", request1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Duplicate VIN with different plate number
        var request2 = new CreateVehicleRequest("1HGCR2F83HA777777", "PLATE-2", "Honda", "Civic", 2020, "Sedan");
        var res2 = await _client.PostAsJsonAsync("/api/v1/vehicles", request2);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateVehicle_WhenPlateNumberAlreadyExistsForTenant_ShouldReturn409Conflict()
    {
        var request1 = new CreateVehicleRequest("1HGCR2F83HA888881", "SAME-PLATE", "Honda", "Civic", 2020, "Sedan");
        var res1 = await _client.PostAsJsonAsync("/api/v1/vehicles", request1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Duplicate Plate with different VIN
        var request2 = new CreateVehicleRequest("1HGCR2F83HA888882", "SAME-PLATE", "Toyota", "Corolla", 2021, "Sedan");
        var res2 = await _client.PostAsJsonAsync("/api/v1/vehicles", request2);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
