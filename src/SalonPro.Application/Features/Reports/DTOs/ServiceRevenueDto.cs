namespace SalonPro.Application.Features.Reports.DTOs;

public record ServiceRevenueDto(
    string ServiceName,
    string Category,
    decimal TotalRevenue,
    int BookingCount,
    decimal AveragePrice
);
