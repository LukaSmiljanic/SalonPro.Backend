namespace SalonPro.Application.Features.Settings.DTOs;

public record TenantBrandingDto(
    string Slug,
    string Name,
    string? LogoUrl,
    string? PrimaryColor,
    string? AccentColor,
    bool OnlineBookingEnabled,
    bool CanUseOnlineBooking,
    string? PublicBookingUrl);

public record UpdateTenantBrandingRequest(
    string? PrimaryColor,
    string? AccentColor,
    bool OnlineBookingEnabled);
