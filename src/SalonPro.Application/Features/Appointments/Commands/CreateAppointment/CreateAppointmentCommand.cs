using MediatR;

namespace SalonPro.Application.Features.Appointments.Commands.CreateAppointment;

public record CreateAppointmentCommand(
    Guid ClientId,
    Guid StaffMemberId,
    DateTime StartTime,
    List<Guid> ServiceIds,
    string? Notes
) : IRequest<Guid>;
