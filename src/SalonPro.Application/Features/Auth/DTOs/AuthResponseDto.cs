using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Auth.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    /// <summary>Nested user for frontend (id, email, name, role, tenantId, tenantName).</summary>
    public AuthUserDto User { get; set; } = null!;
}
