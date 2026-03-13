namespace SalonPro.Application.Features.Settings.DTOs;

public record LoyaltyConfigDto(
    string TierName,
    int MinVisits,
    string Benefit
);
