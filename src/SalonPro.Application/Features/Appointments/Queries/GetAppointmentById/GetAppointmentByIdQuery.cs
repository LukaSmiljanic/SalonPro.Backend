using MediatR;
using SalonPro.Application.Features.Appointments.DTOs;

namespace SalonPro.Application.Features.Appointments.Queries.GetAppointmentById;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<AppointmentDetailDto>;
