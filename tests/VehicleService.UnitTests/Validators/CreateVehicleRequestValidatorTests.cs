using FluentValidation.TestHelper;
using Fleet.VehicleService.Application.DTOs;
using Fleet.VehicleService.Application.Validators;
using Xunit;

namespace VehicleService.UnitTests.Validators;

public class CreateVehicleRequestValidatorTests
{
    private readonly CreateVehicleRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenAllFieldsValid_ShouldNotHaveAnyErrors()
    {
        var request = new CreateVehicleRequest(
            Vin: "1HGCR2F83HA123456",
            PlateNumber: "NYC-4321",
            Make: "Toyota",
            Model: "Camry",
            Year: 2022,
            Type: "Sedan"
        );

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("SHORTVIN")]
    [InlineData("1HGCR2F83HA12345678")] // 18 chars (too long)
    [InlineData("1HGCR2F83HA12345!")] // Invalid character
    public void Validate_WhenVinInvalid_ShouldHaveValidationError(string invalidVin)
    {
        var request = new CreateVehicleRequest(
            Vin: invalidVin,
            PlateNumber: "NYC-4321",
            Make: "Toyota",
            Model: "Camry",
            Year: 2022,
            Type: "Sedan"
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Vin);
    }

    [Fact]
    public void Validate_WhenYearInFarFuture_ShouldHaveValidationError()
    {
        var request = new CreateVehicleRequest(
            Vin: "1HGCR2F83HA123456",
            PlateNumber: "NYC-4321",
            Make: "Toyota",
            Model: "Camry",
            Year: DateTime.UtcNow.Year + 5,
            Type: "Sedan"
        );

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }
}
