using MediatR;
using Microsoft.Extensions.Configuration;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Settings.Queries.GetTenantBranding;

public record GetTenantBrandingQuery : IRequest<TenantBrandingDto>;

public class GetTenantBrandingQueryHandler : IRequestHandler<GetTenantBrandingQuery, TenantBrandingDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMediaStorageService _mediaStorage;
    private readonly IConfiguration _configuration;

    public GetTenantBrandingQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IMediaStorageService mediaStorage,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _mediaStorage = mediaStorage;
        _configuration = configuration;
    }

    public async Task<TenantBrandingDto> Handle(GetTenantBrandingQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Salon nije pronađen.");

        var logoUrl = ResolveLogoUrl(tenant.LogoUrl);
        var canBook = TenantPlanRules.CanUseOnlineBooking(tenant.Plan);
        var bookingBase = (_configuration["AppSettings:BookingWebUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(bookingBase))
        {
            var frontend = (_configuration["AppSettings:FrontendUrl"] ?? "https://salonpro.netlify.app").TrimEnd('/');
            bookingBase = $"{frontend}/book";
        }
        var publicUrl = string.IsNullOrWhiteSpace(tenant.Slug)
            ? null
            : $"{bookingBase}/{tenant.Slug}";

        return new TenantBrandingDto(
            tenant.Slug,
            tenant.Name,
            logoUrl,
            tenant.PrimaryColor ?? "#5b3a8c",
            tenant.AccentColor ?? "#8b5cf6",
            tenant.OnlineBookingEnabled,
            canBook,
            publicUrl);
    }

    private string? ResolveLogoUrl(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return null;

        var trimmed = stored.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (trimmed.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        return _mediaStorage.ToRelativeMediaPath(trimmed.TrimStart('/'));
    }
}
