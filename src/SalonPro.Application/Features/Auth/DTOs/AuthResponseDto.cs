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

    /// <summary>Tenant plan: Basic, Standard, Pro.</summary>
    public string? Plan { get; set; }

    /// <summary>Feature flags derived from tenant plan.</summary>
    public TenantPlanFeaturesDto? Features { get; set; }
}

public class TenantPlanFeaturesDto
{
    public bool CanUseOnlineBooking { get; set; }
    public int MaxStaffMembers { get; set; }
}
