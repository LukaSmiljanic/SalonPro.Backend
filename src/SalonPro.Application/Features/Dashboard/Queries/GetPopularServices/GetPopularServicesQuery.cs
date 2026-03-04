using MediatR;
using SalonPro.Application.Features.Dashboard.DTOs;
using SalonPro.Application.Features.Dashboard.Queries.GetRevenueChart;

namespace SalonPro.Application.Features.Dashboard.Queries.GetPopularServices;

public record GetPopularServicesQuery(ChartPeriod Period) : IRequest<List<PopularServiceDto>>;
