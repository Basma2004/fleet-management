using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Fleet.VehicleService.Application.DTOs;
using Xunit;

namespace VehicleService.IntegrationTests;
[Collection("VehicleApiCollection")]
public class DeviceBindingTests 
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeviceBindingTests(VehicleApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenantId.ToString());
    }

    [Fact]
    public async Task BindDevice_WhenSameDeviceAssignedTwice_ShouldReturn409Conflict()
    {
        // Generate unique VINs and Device ID for this specific test run
        var vin1 = $"1HGCR{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        var vin2 = $"1HGCR{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        var deviceId = $"DEV-{Guid.NewGuid():N}"[..16];

        // 1. Create two vehicles
        var vehicle1Response = await _client.PostAsJsonAsync("/api/v1/vehicles", new CreateVehicleRequest(
            vin1, "DEV-01", "Ford", "Transit", 2022, "Van"));
        var v1 = await vehicle1Response.Content.ReadFromJsonAsync<VehicleResponse>();

        var vehicle2Response = await _client.PostAsJsonAsync("/api/v1/vehicles", new CreateVehicleRequest(
            vin2, "DEV-02", "Ford", "Transit", 2022, "Van"));
        var v2 = await vehicle2Response.Content.ReadFromJsonAsync<VehicleResponse>();

        // 2. Bind device to Vehicle 1 -> Should Succeed (200 OK)
        var bind1 = await _client.PostAsJsonAsync($"/api/v1/vehicles/{v1!.Id}/device", new BindDeviceRequest(deviceId));
        bind1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Attempt to bind same device to Vehicle 2 -> Must fail with 409 Conflict
        var bind2 = await _client.PostAsJsonAsync($"/api/v1/vehicles/{v2!.Id}/device", new BindDeviceRequest(deviceId));
        bind2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 4. Unbind from Vehicle 1 -> 204 NoContent
        var unbind = await _client.DeleteAsync($"/api/v1/vehicles/{v1.Id}/device");
        unbind.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Now binding to Vehicle 2 should succeed
        var bindAgain = await _client.PostAsJsonAsync($"/api/v1/vehicles/{v2.Id}/device", new BindDeviceRequest(deviceId));
        bindAgain.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
