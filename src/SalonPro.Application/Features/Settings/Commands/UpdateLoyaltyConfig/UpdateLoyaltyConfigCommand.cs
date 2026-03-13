using MediatR;

namespace SalonPro.Application.Features.Settings.Commands.UpdateLoyaltyConfig;

public record LoyaltyTierItem(
    string TierName,
    int MinVisits,
    string Benefit
);

public record UpdateLoyaltyConfigCommand(List<LoyaltyTierItem> Tiers) : IRequest<Unit>;
