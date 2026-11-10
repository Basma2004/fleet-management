using FluentValidation;
using Fleet.VehicleService.Application.DTOs;

namespace Fleet.VehicleService.Application.Validators;

public class CreateMaintenanceRecordRequestValidator : AbstractValidator<CreateMaintenanceRecordRequest>
{
    public CreateMaintenanceRecordRequestValidator()
    {
        RuleFor(x => x.ServiceDate)
            .NotEmpty().WithMessage("Service date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Service date cannot be in the future.");

        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("Service type is required.")
            .MaximumLength(100).WithMessage("Service type cannot exceed 100 characters.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue)
            .WithMessage("Cost cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}