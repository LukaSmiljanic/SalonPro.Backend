using MediatR;
using SalonPro.Application.Features.Dashboard.DTOs;

namespace SalonPro.Application.Features.Dashboard.Queries.GetDashboardStats;

public record GetDashboardStatsQuery(DateTime? Date = null) : IRequest<DashboardStatsDto>;
