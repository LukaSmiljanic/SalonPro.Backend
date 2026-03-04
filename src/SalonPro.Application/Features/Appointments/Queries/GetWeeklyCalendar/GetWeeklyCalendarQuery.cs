using MediatR;
using SalonPro.Application.Features.Appointments.DTOs;

namespace SalonPro.Application.Features.Appointments.Queries.GetWeeklyCalendar;

public record GetWeeklyCalendarQuery(
    DateTime WeekStartDate,
    Guid? StaffMemberId = null
) : IRequest<WeeklyCalendarDto>;
