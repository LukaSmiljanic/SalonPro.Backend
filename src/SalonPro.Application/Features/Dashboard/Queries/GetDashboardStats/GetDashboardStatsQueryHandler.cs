using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var targetDate = (request.Date ?? DateTime.UtcNow).Date;
        var targetDateEnd = targetDate.AddDays(1);
        var yesterday = targetDate.AddDays(-1);
        var thisMonthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart;

        // Today's revenue from completed appointments
        var todayRevenue = await _unitOfWork.Appointments.Query()
            .Where(a =>
                a.StartTime >= targetDate &&
                a.StartTime < targetDateEnd &&
                a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        // Yesterday's revenue for comparison
        var yesterdayRevenue = await _unitOfWork.Appointments.Query()
            .Where(a =>
                a.StartTime >= yesterday &&
                a.StartTime < targetDate &&
                a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        var revenueChangePercent = yesterdayRevenue == 0
            ? (todayRevenue > 0 ? 100m : 0m)
            : Math.Round((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100, 1);

        // Appointments today
        var appointmentsToday = await _unitOfWork.Appointments.Query()
            .CountAsync(a =>
                a.StartTime >= targetDate &&
                a.StartTime < targetDateEnd &&
                a.Status != AppointmentStatus.Cancelled,
                cancellationToken);

        // Pending appointments today
        var appointmentsPending = await _unitOfWork.Appointments.Query()
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

        var staffWithAppointmentsToday = await _unitOfWork.Appointments.Query()
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
        var staffWithAppointmentsYesterday = await _unitOfWork.Appointments.Query()
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

        return new DashboardStatsDto(
            todayRevenue,
            revenueChangePercent,
            appointmentsToday,
            appointmentsPending,
            newClientsThisMonth,
            newClientsChangePercent,
            occupancyRatePercent,
            occupancyChangePercent
        );
    }
}
