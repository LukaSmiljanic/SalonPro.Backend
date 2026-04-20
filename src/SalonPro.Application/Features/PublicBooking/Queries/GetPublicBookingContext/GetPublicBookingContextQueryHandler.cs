using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common;
using SalonPro.Application.Features.PublicBooking.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.PublicBooking.Queries.GetPublicBookingContext;

public class GetPublicBookingContextQueryHandler : IRequestHandler<GetPublicBookingContextQuery, PublicBookingContext?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicBookingContextQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var salon = new PublicBookingSalonDto(
            tenant.Slug,
            tenant.Name,
            tenant.LogoUrl,
            tenant.City,
            tenant.Phone,
            tenant.Address,
            string.IsNullOrWhiteSpace(tenant.Currency) ? "RSD" : tenant.Currency
        );

        return new PublicBookingContext(tenant.Id, TenantPlanRules.Normalize(tenant.Plan), salon);
    }
}
