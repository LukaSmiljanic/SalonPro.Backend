namespace SalonPro.Application.Features.PublicBooking.DTOs;

/// <summary>Salon info exposed to the public booking site (no internal IDs).</summary>
public record PublicBookingSalonDto(
    string Slug,
    string Name,
    string? LogoUrl,
    string? City,
    string? Phone,
    string? Address,
    string Currency
);

public record PublicBookingContext(
    Guid TenantId,
    string Plan,
    PublicBookingSalonDto Salon
);
