namespace SalonPro.Application.Features.Tenants.DTOs;

public record TenantListDto(
    Guid Id,
    string Name,
    string Slug,
    string? Email,
    string? Phone,
    string? City,
    bool IsActive,
    bool EmailVerified,
    string SubscriptionStatus,
    DateTime? SubscriptionEndDate,
    int? DaysRemaining,
    int UserCount,
    int ClientCount,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
