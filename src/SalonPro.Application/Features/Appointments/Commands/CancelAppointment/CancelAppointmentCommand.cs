using MediatR;

namespace SalonPro.Application.Features.Appointments.Commands.CancelAppointment;

public record CancelAppointmentCommand(
    Guid Id,
    string? CancellationReason
) : IRequest<Unit>;
