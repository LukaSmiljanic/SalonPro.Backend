using MediatR;
using SalonPro.Application.Features.Dashboard.DTOs;

namespace SalonPro.Application.Features.Dashboard.Queries.GetRevenueChart;

public enum ChartPeriod
{
    Week = 0,
    Month = 1
}

public record GetRevenueChartQuery(ChartPeriod Period, int? Days = null) : IRequest<RevenueChartDto>;
