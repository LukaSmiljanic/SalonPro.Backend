using MediatR;
using SalonPro.Application.Features.Insights.DTOs;

namespace SalonPro.Application.Features.Insights.Queries.GetDashboardInsights;

public record GetDashboardInsightsQuery() : IRequest<DashboardInsightsDto>;
