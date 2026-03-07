using MediatR;
using SalonPro.Application.Features.Insights.DTOs;

namespace SalonPro.Application.Features.Insights.Queries.GetClientInsights;

public record GetClientInsightsQuery(Guid ClientId) : IRequest<ClientInsightsDto>;
