using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.PublicBooking.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.PublicBooking.Queries.GetPublicBookingContext;

public class GetPublicBookingContextQueryHandler : IRequestHandler<GetPublicBookingContextQuery, PublicBookingContext?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorageService _mediaStorage;

    public GetPublicBookingContextQueryHandler(IUnitOfWork unitOfWork, IMediaStorageService mediaStorage)
    {
        _unitOfWork = unitOfWork;
        _mediaStorage = mediaStorage;
    }

    public async Task<PublicBookingContext?> Handle(GetPublicBookingContextQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim();
        if (string.IsNullOrEmpty(slug))
            return null;

        var slugLower = slug.ToLowerInvariant();
        var tenant = await _unitOfWork.Tenants.Query()
            .AsNoTracking()
            .Where(t => t.Slug.ToLower() == slugLower && t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.LogoUrl,
                t.PrimaryColor,
                t.AccentColor,
                t.OnlineBookingEnabled,
                t.City,
                t.Phone,
                t.Address,
                t.Currency,
                t.Plan,
                t.SubscriptionEndDate,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
            return null;

        var subscriptionOk = tenant.SubscriptionEndDate.HasValue &&
                             tenant.SubscriptionEndDate.Value > DateTime.UtcNow;
        if (!subscriptionOk)
            return null;

        var logoUrl = ResolveLogoUrl(tenant.LogoUrl);
        var planAllows = TenantPlanRules.CanUseOnlineBooking(tenant.Plan);

        var salon = new PublicBookingSalonDto(
            tenant.Slug,
            tenant.Name,
            logoUrl,
            tenant.City,
            tenant.Phone,
            tenant.Address,
            string.IsNullOrWhiteSpace(tenant.Currency) ? "RSD" : tenant.Currency,
            tenant.PrimaryColor ?? "#5b3a8c",
            tenant.AccentColor ?? "#8b5cf6",
            planAllows && tenant.OnlineBookingEnabled
        );

        return new PublicBookingContext(tenant.Id, TenantPlanRules.Normalize(tenant.Plan), salon);
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
