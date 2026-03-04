using MediatR;
using SalonPro.Application.Features.Appointments.DTOs;

namespace SalonPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;

public record GetAppointmentsByDateQuery(
    DateTime Date,
    Guid? StaffMemberId = null
) : IRequest<List<AppointmentDto>>;
