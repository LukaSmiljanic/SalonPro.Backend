using FluentValidation;

namespace SalonPro.Application.Features.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required.");

        RuleFor(x => x.StaffMemberId)
            .NotEmpty().WithMessage("Staff member ID is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5)).WithMessage("Start time must be in the future.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("At least one service must be selected.")
            .Must(ids => ids.Count > 0).WithMessage("At least one service must be selected.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes != null);
    }
}
