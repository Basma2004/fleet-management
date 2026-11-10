using FluentValidation;
using Fleet.VehicleService.Application.DTOs;

namespace Fleet.VehicleService.Application.Validators;

public class UpdateOdometerRequestValidator : AbstractValidator<UpdateOdometerRequest>
{
    private static readonly string[] AllowedSources = ["manual", "telemetry"];

    public UpdateOdometerRequestValidator()
    {
        RuleFor(x => x.OdometerKm)
            .GreaterThanOrEqualTo(0).WithMessage("Odometer reading cannot be negative.");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Update source is required.")
            .Must(s => AllowedSources.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("Source must be either 'manual' or 'telemetry'.");
    }
}