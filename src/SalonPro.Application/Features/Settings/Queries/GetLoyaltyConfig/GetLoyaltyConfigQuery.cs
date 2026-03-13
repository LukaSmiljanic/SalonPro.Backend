using MediatR;
using SalonPro.Application.Features.Settings.DTOs;

namespace SalonPro.Application.Features.Settings.Queries.GetLoyaltyConfig;

public record GetLoyaltyConfigQuery() : IRequest<List<LoyaltyConfigDto>>;
