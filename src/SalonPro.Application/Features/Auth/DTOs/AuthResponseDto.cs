using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Auth.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    /// <summary>Nested user for frontend (id, email, name, role, tenantId, tenantName).</summary>
    public AuthUserDto User { get; set; } = null!;

    /// <summary>True when registration succeeded but email verification is still pending.</summary>
    public bool RequiresEmailVerification { get; set; } = false;

    /// <summary>Subscription status: Active, Trial, Expired, null if not applicable.</summary>
    public string? SubscriptionStatus { get; set; }

    /// <summary>When subscription/trial expires (UTC).</summary>
    public DateTime? SubscriptionEndDate { get; set; }
}
