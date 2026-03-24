using FluentValidation;

namespace SalonPro.Application.Features.Appointments.Commands.UpdateAppointment;

public class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID termina je obavezan.");

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Klijent je obavezan.");

        RuleFor(x => x.StaffMemberId)
            .NotEmpty().WithMessage("Zaposleni je obavezan.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Vreme početka je obavezno.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Izaberite bar jednu uslugu.")
            .Must(ids => ids.Count > 0).WithMessage("Izaberite bar jednu uslugu.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Napomene ne smeju biti duže od 1000 karaktera.")
            .When(x => x.Notes != null);
    }
}
