using FluentValidation;
using Fleet.VehicleService.Application.DTOs;

namespace Fleet.VehicleService.Application.Validators;

public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("Plate number is required.")
            .MaximumLength(20).WithMessage("Plate number cannot exceed 20 characters.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .When(x => x.Year.HasValue)
            .WithMessage($"Year must be between 1900 and {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.Make)
            .MaximumLength(50).WithMessage("Make cannot exceed 50 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(50).WithMessage("Model cannot exceed 50 characters.");

        RuleFor(x => x.Type)
            .MaximumLength(30).WithMessage("Vehicle type cannot exceed 30 characters.");
    }
}