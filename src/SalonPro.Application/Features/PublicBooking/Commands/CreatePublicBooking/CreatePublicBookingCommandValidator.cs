using FluentValidation;

namespace SalonPro.Application.Features.PublicBooking.Commands.CreatePublicBooking;

public class CreatePublicBookingCommandValidator : AbstractValidator<CreatePublicBookingCommand>
{
    public CreatePublicBookingCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).MaximumLength(200);
        RuleFor(x => x.StaffMemberId).NotEmpty();
        RuleFor(x => x.ServiceIds).NotEmpty();
        RuleForEach(x => x.ServiceIds).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
