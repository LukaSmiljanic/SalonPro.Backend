using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Appointments.Queries.GetWeeklyCalendar;

public class GetWeeklyCalendarQueryHandler : IRequestHandler<GetWeeklyCalendarQuery, WeeklyCalendarDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWeeklyCalendarQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WeeklyCalendarDto> Handle(GetWeeklyCalendarQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.WeekStartDate.Date;
        var endDate = startDate.AddDays(7);

        var query = _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.AppointmentServices)
                .ThenInclude(aps => aps.Service)
            .Where(a => a.StartTime >= startDate && a.StartTime < endDate)
            .AsNoTracking();

        if (request.StaffMemberId.HasValue)
        {
            query = query.Where(a => a.StaffMemberId == request.StaffMemberId.Value);
        }

        var appointments = await query
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

        var days = Enumerable.Range(0, 7).Select(i =>
        {
            var date = startDate.AddDays(i);
            var dayAppointments = appointments
                .Where(a => a.StartTime.Date == date.Date)
                .Select(a => new AppointmentDto(
                    a.Id,
                    a.Client.FullName,
                    a.StaffMember.FullName,
                    a.StartTime,
                    a.EndTime,
                    a.Status,
                    a.TotalPrice,
                    a.Notes,
                    a.AppointmentServices.Select(aps => new AppointmentServiceDto(
                        aps.ServiceId,
                        aps.Service.Name,
                        aps.Price,
                        aps.DurationMinutes
                    )).ToList()
                )).ToList();

            return new CalendarDayDto(date, dayAppointments);
        }).ToList();

        return new WeeklyCalendarDto(startDate, endDate.AddDays(-1), days);
    }
}
