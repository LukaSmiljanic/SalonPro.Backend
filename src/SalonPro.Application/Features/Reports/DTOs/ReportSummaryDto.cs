namespace SalonPro.Application.Features.Reports.DTOs;

public record ReportSummaryDto(
    decimal TotalRevenue,
    int TotalAppointments,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    decimal CancellationRate,
    decimal NoShowRate,
    int UniqueClients,
    decimal AverageRevenuePerDay
);
