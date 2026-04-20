using MediatR;

namespace SalonPro.Application.Features.PublicBooking.Commands.CreatePublicBooking;

public record CreatePublicBookingCommand(
    string FirstName,
    string LastName,
    string Phone,
    string? Email,
    Guid StaffMemberId,
    List<Guid> ServiceIds,
    DateTime StartTime,
    string? Notes
) : IRequest<Guid>;
