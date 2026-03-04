using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Enums;
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
        var weekStart = request.WeekStartDate.Date;
        var weekEnd = weekStart.AddDays(7);

        var appointments = await _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.StaffMember)
            .Include(a => a.AppointmentServices)
                .ThenInclude(aps => aps.Service)
                    .ThenInclude(s => s.Category)
            .Where(a => a.StartTime >= weekStart && a.StartTime < weekEnd)
            .OrderBy(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var staffSchedules = appointments
            .GroupBy(a => a.StaffMemberId)
            .Select(g =>
            {
                var staffMember = g.First().StaffMember;
                var calendarAppointments = g.Select(a =>
                {
                    var firstService = a.AppointmentServices.FirstOrDefault();
                    var category = firstService?.Service.Category;
                    return new CalendarAppointmentDto(
                        a.Id,
                        a.Client.FullName,
                        firstService?.Service.Name ?? string.Empty,
                        a.StartTime,
                        a.EndTime,
                        category?.Type ?? ServiceCategoryType.Other,
                        category?.ColorHex ?? "#CCCCCC"
                    );
                }).ToList();

                return new StaffCalendarDto(
                    staffMember.Id,
                    staffMember.FullName,
                    calendarAppointments.Count,
                    calendarAppointments
                );
            })
            .ToList();

        return new WeeklyCalendarDto(staffSchedules);
    }
}
