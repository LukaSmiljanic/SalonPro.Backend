using MediatR;
using SalonPro.Application.Features.Appointments.DTOs;

namespace SalonPro.Application.Features.Appointments.Queries.GetAppointmentsByStaff;

public record GetAppointmentsByStaffQuery(
    Guid StaffMemberId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<List<AppointmentDto>>;
