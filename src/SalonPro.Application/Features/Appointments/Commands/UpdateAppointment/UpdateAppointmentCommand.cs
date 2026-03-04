using MediatR;
using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Appointments.Commands.UpdateAppointment;

public record UpdateAppointmentCommand(
    Guid Id,
    Guid ClientId,
    Guid StaffMemberId,
    DateTime StartTime,
    List<Guid> ServiceIds,
    string? Notes,
    AppointmentStatus Status
) : IRequest<Unit>;
