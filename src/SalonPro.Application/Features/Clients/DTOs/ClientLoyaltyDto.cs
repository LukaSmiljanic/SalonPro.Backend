namespace SalonPro.Application.Features.Clients.DTOs;

public record ClientLoyaltyDto(
    int TotalVisits,
    string LoyaltyTier,
    string? LoyaltyBenefit,
    int? NextMilestone,
    int VisitsUntilNextMilestone,
    string? NextMilestoneBenefit
);
