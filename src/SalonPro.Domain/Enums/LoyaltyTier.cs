namespace SalonPro.Domain.Enums;

public enum LoyaltyTier
{
    None = 0,
    Bronze = 1,   // 10+ visits → 20% off
    Silver = 2,   // 25+ visits → 30% off
    Gold = 3,     // 50+ visits → free service
    Platinum = 4  // 100+ visits → free service + gift
}
