using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetDashboardStatsQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var targetDate = (request.Date ?? DateTime.UtcNow).Date;
        var targetDateEnd = targetDate.AddDays(1);
        var yesterday = targetDate.AddDays(-1);
        var thisMonthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart;

        // All appointment queries explicitly scoped to current tenant (defence in depth)
        var appointmentsBase = _unitOfWork.Appointments.Query().Where(a => a.TenantId == tenantId);

        // Today's revenue from completed appointments
        var todayRevenue = await appointmentsBase
            .Where(a =>
                a.StartTime >= targetDate &&
                a.StartTime < targetDateEnd &&
                a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        // Yesterday's revenue for comparison
        var yesterdayRevenue = await appointmentsBase
            .Where(a =>
                a.StartTime >= yesterday &&
                a.StartTime < targetDate &&
                a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        var revenueChangePercent = yesterdayRevenue == 0
            ? (todayRevenue > 0 ? 100m : 0m)
            : Math.Round((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100, 1);

        // Appointments today
        var appointmentsToday = await appointmentsBase
            .CountAsync(a =>
                a.StartTime >= targetDate &&
                a.StartTime < targetDateEnd &&
                a.Status != AppointmentStatus.Cancelled,
                cancellationToken);

        // Pending appointments today
        var appointmentsPending = await appointmentsBase
            .CountAsync(a =>
                a.StartTime >= targetDate &&
                a.StartTime < targetDateEnd &&
                (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed),
                cancellationToken);

        // New clients this month
        var newClientsThisMonth = await _unitOfWork.Clients.Query()
            .CountAsync(c => c.CreatedAt >= thisMonthStart && c.CreatedAt < targetDateEnd, cancellationToken);

        // New clients last month for comparison
        var newClientsLastMonth = await _unitOfWork.Clients.Query()
            .CountAsync(c => c.CreatedAt >= lastMonthStart && c.CreatedAt < lastMonthEnd, cancellationToken);

        var newClientsChangePercent = newClientsLastMonth == 0
            ? (newClientsThisMonth > 0 ? 100m : 0m)
            : Math.Round((decimal)(newClientsThisMonth - newClientsLastMonth) / newClientsLastMonth * 100, 1);

        // Occupancy rate: active staff with appointments today / total active staff
        var totalActiveStaff = await _unitOfWork.StaffMembers.Query()
            .CountAsync(s => s.IsActive, cancellationToken);

        var staffWithAppointmentsToday = await appointmentsBase
            .Where(a =>
                a.StartTime >= targetDate &&
                a.StartTime < targetDateEnd &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StaffMemberId)
            .Distinct()
            .CountAsync(cancellationToken);

        var occupancyRatePercent = totalActiveStaff == 0
            ? 0m
            : Math.Round((decimal)staffWithAppointmentsToday / totalActiveStaff * 100, 1);

        // Yesterday occupancy for comparison
        var staffWithAppointmentsYesterday = await appointmentsBase
            .Where(a =>
                a.StartTime >= yesterday &&
                a.StartTime < targetDate &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StaffMemberId)
            .Distinct()
            .CountAsync(cancellationToken);

        var yesterdayOccupancy = totalActiveStaff == 0
            ? 0m
            : Math.Round((decimal)staffWithAppointmentsYesterday / totalActiveStaff * 100, 1);

        var occupancyChangePercent = yesterdayOccupancy == 0
            ? (occupancyRatePercent > 0 ? 100m : 0m)
            : Math.Round((occupancyRatePercent - yesterdayOccupancy) / yesterdayOccupancy * 100, 1);

        // Week revenue (last 7 days, completed only)
        var weekStart = targetDate.AddDays(-6);
        var weekRevenue = await appointmentsBase
            .Where(a =>
                a.StartTime >= weekStart &&
                a.StartTime < targetDateEnd &&
                a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        // Total clients (all time)
        var totalClients = await _unitOfWork.Clients.Query()
            .CountAsync(c => c.IsActive, cancellationToken);

        // Completion rate this month: completed / (total not cancelled) * 100
        var monthEnd = thisMonthStart.AddMonths(1);
        var completedThisMonth = await appointmentsBase
            .CountAsync(a =>
                a.StartTime >= thisMonthStart &&
                a.StartTime < monthEnd &&
                a.Status == AppointmentStatus.Completed,
                cancellationToken);
        var totalThisMonth = await appointmentsBase
            .CountAsync(a =>
                a.StartTime >= thisMonthStart &&
                a.StartTime < monthEnd &&
                a.Status != AppointmentStatus.Cancelled,
                cancellationToken);
        var completionRate = totalThisMonth == 0 ? 0m : Math.Round((decimal)completedThisMonth / totalThisMonth * 100, 1);

        // Upcoming appointments (next 20 from now, not cancelled)
        var now = request.Date ?? DateTime.UtcNow;
        var upcoming = await appointmentsBase
            .Where(a => a.StartTime >= now && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .Take(20)
            .Select(a => new
            {
                a.Id,
                ClientName = a.Client!.FullName,
                ServiceNames = a.AppointmentServices.Select(aps => aps.Service.Name),
                StaffName = a.StaffMember!.FullName,
                a.StartTime,
                a.Status,
            })
            .ToListAsync(cancellationToken);

        var upcomingDtos = upcoming.Select(a => new UpcomingAppointmentDto(
            a.Id,
            a.ClientName,
            string.Join(", ", a.ServiceNames),
            a.StaffName,
            a.StartTime,
            a.Status.ToString()
        )).ToList();

        // Birthday reminders — clients with birthdays in the next 7 days
        var clientsWithBirthday = await _unitOfWork.Clients.Query()
            .Where(c => c.TenantId == tenantId && c.IsActive && c.DateOfBirth.HasValue)
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                FullName = c.FirstName + " " + c.LastName,
                c.Phone,
                c.Email,
                DateOfBirth = c.DateOfBirth!.Value
            })
            .ToListAsync(cancellationToken);

        var birthdayReminders = new List<BirthdayReminderDto>();
        foreach (var client in clientsWithBirthday)
        {
            var birthdayThisYear = new DateTime(targetDate.Year, client.DateOfBirth.Month, client.DateOfBirth.Day);
            if (birthdayThisYear < targetDate)
                birthdayThisYear = birthdayThisYear.AddYears(1);

            var daysUntil = (birthdayThisYear - targetDate).Days;
            if (daysUntil <= 7)
            {
                var age = birthdayThisYear.Year - client.DateOfBirth.Year;
                birthdayReminders.Add(new BirthdayReminderDto(
                    client.Id,
                    client.FullName,
                    client.Phone,
                    client.Email,
                    client.DateOfBirth,
                    daysUntil,
                    age
                ));
            }
        }
        birthdayReminders = birthdayReminders.OrderBy(r => r.DaysUntilBirthday).ToList();

        return new DashboardStatsDto(
            todayRevenue,
            revenueChangePercent,
            appointmentsToday,
            appointmentsPending,
            newClientsThisMonth,
            newClientsChangePercent,
            occupancyRatePercent,
            occupancyChangePercent,
            weekRevenue,
            totalClients,
            completionRate,
            upcomingDtos,
            birthdayReminders
        );
    }
}
