using Xunit;

namespace VehicleService.IntegrationTests;

[CollectionDefinition("VehicleApiCollection")]
public class VehicleApiCollection : ICollectionFixture<VehicleApiFactory>
{
    // This class has no code. Its sole purpose is to anchor the shared fixture
    // across all integration test suites.
}
