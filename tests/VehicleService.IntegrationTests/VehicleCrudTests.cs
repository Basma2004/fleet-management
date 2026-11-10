using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Fleet.Shared.Domain;
using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Domain.Enums;
using Xunit;

namespace VehicleService.IntegrationTests;

public class VehicleCrudTests : IClassFixture<VehicleApiFactory>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.NewGuid();

    public VehicleCrudTests(VehicleApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenantId.ToString());
    }

    [Fact]
    public async Task VehicleCrudLifecycle_ShouldPerformFullCycleSuccessfully()
    {
        // 1. Create Vehicle (POST)
        var createRequest = new CreateVehicleRequest(
            Vin: "1HGCR2F83HA987654",
            PlateNumber: "TEX-9988",
            Make: "Tesla",
            Model: "Model Y",
            Year: 2023,
            Type: "Electric SUV"
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/vehicles", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        created.Should().NotBeNull();
        created!.Vin.Should().Be("1HGCR2F83HA987654");
        created.Status.Should().Be(VehicleStatus.Active);

        // 2. Get Vehicle by ID (GET)
        var getResponse = await _client.GetAsync($"/api/v1/vehicles/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        fetched!.PlateNumber.Should().Be("TEX-9988");

        // 3. Update Vehicle (PUT)
        var updateRequest = new UpdateVehicleRequest(
            Make: "Tesla",
            Model: "Model Y Long Range",
            Year: 2024,
            Type: "Electric SUV",
            PlateNumber: "TEX-9988"
        );

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/vehicles/{created.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        updated!.Model.Should().Be("Model Y Long Range");
        updated.Year.Should().Be(2024);

        // 4. Soft Delete / Retire (DELETE)
        var deleteResponse = await _client.DeleteAsync($"/api/v1/vehicles/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status changed to Retired
        var verifyResponse = await _client.GetAsync($"/api/v1/vehicles/{created.Id}");
        var retired = await verifyResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        retired!.Status.Should().Be(VehicleStatus.Retired);
    }

    [Fact]
    public async Task GetAll_ShouldReturnPaginatedAndFilteredList()
    {
        // Arrange: Create a specific van to test filtering
        var createRequest = new CreateVehicleRequest(
            Vin: "1HGCR2F83HA112233",
            PlateNumber: "VAN-001",
            Make: "Mercedes",
            Model: "Sprinter",
            Year: 2022,
            Type: "Cargo Van"
        );
        await _client.PostAsJsonAsync("/api/v1/vehicles", createRequest);

        // Act: Filter by Type=Cargo Van
        var response = await _client.GetAsync("/api/v1/vehicles?type=Cargo Van&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<VehicleResponse>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().Contain(v => v.PlateNumber == "VAN-001");
    }
}
