using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Fleet.Shared.Domain;
using Fleet.VehicleService.Application.DTOs;
using Xunit;

namespace VehicleService.IntegrationTests;
[Collection("VehicleApiCollection")]
public class MaintenanceHistoryTests 
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.NewGuid();

    public MaintenanceHistoryTests(VehicleApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenantId.ToString());
    }

    [Fact]
    public async Task AddMaintenanceRecord_ShouldPersistAndReturnHistory()
    {
        // Generate unique VIN and Plate for this test run
        var vin = $"1HGCR{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        var plate = $"MNT-{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        // 1. Create Vehicle
        var vehicleRes = await _client.PostAsJsonAsync("/api/v1/vehicles", new CreateVehicleRequest(
            vin, plate, "Volvo", "XC90", 2023, "SUV"));
        vehicleRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = await vehicleRes.Content.ReadFromJsonAsync<VehicleResponse>();

        // 2. Add Maintenance Record (POST)
        var mntRequest = new CreateMaintenanceRecordRequest(
            ServiceDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            ServiceType: "Annual Inspection & Transmission Flush",
            Cost: 450.75m,
            Notes: "Passed all emission checks."
        );

        var recordRes = await _client.PostAsJsonAsync($"/api/v1/vehicles/{vehicle!.Id}/maintenance", mntRequest);
        recordRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var record = await recordRes.Content.ReadFromJsonAsync<MaintenanceRecordResponse>();
        record.Should().NotBeNull();
        record!.ServiceType.Should().Be("Annual Inspection & Transmission Flush");
        record.Cost.Should().Be(450.75m);

        // 3. Query Maintenance History (GET)
        var historyRes = await _client.GetAsync($"/api/v1/vehicles/{vehicle.Id}/maintenance?page=1&pageSize=10");
        historyRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await historyRes.Content.ReadFromJsonAsync<PagedResult<MaintenanceRecordResponse>>();
        history!.Items.Should().ContainSingle(m => m.ServiceType == "Annual Inspection & Transmission Flush");
    }
}
