namespace SalonPro.Application.Features.Dashboard.DTOs;

public record DashboardStatsDto(
    decimal TodayRevenue,
    decimal RevenueChangePercent,
    int AppointmentsToday,
    int AppointmentsPending,
    int NewClientsThisMonth,
    decimal NewClientsChangePercent,
    decimal OccupancyRatePercent,
    decimal OccupancyChangePercent,
    decimal WeekRevenue,
    int TotalClients,
    decimal CompletionRate,
    List<UpcomingAppointmentDto> UpcomingAppointments
);
