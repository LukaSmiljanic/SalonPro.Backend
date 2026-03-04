namespace SalonPro.Application.Features.Dashboard.DTOs;

public record RevenueDataPoint(
    string Label,
    decimal Value,
    DateTime Date
);

public record RevenueChartDto(List<RevenueDataPoint> DataPoints);
