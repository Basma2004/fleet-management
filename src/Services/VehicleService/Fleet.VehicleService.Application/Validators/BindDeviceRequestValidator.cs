using FluentValidation;
using Fleet.VehicleService.Application.DTOs;

namespace Fleet.VehicleService.Application.Validators;

public class BindDeviceRequestValidator : AbstractValidator<BindDeviceRequest>
{
    public BindDeviceRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("Device ID is required.")
            .MaximumLength(50).WithMessage("Device ID cannot exceed 50 characters.");
    }
}