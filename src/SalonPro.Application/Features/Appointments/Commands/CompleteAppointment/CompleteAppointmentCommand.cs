using MediatR;

namespace SalonPro.Application.Features.Appointments.Commands.CompleteAppointment;

public record CompleteAppointmentCommand(Guid Id) : IRequest<Unit>;
