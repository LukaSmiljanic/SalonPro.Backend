using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Tenants.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, List<TenantListDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTenantsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TenantListDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var tenants = await _unitOfWork.Tenants.Query()
            .AsNoTracking()
            .Include(t => t.Users)
            .Include(t => t.Clients)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tenants.Select(t =>
        {
            string subStatus;
            int? daysRemaining = null;

            if (!t.EmailVerified)
            {
                subStatus = "ČekaVerifikaciju";
            }
            else if (t.IsTrialing && t.HasActiveSubscription)
            {
                subStatus = "Probni";
                daysRemaining = (int)(t.SubscriptionEndDate!.Value - now).TotalDays;
            }
            else if (t.HasActiveSubscription)
            {
                subStatus = "Aktivan";
                daysRemaining = (int)(t.SubscriptionEndDate!.Value - now).TotalDays;
            }
            else
            {
                subStatus = "Istekao";
                daysRemaining = 0;
            }

            // Last login = most recent RefreshTokenExpiry minus 7 days, or fallback to user CreatedAt
            var lastLogin = t.Users
                .Where(u => u.RefreshTokenExpiry.HasValue)
                .OrderByDescending(u => u.RefreshTokenExpiry)
                .Select(u => u.RefreshTokenExpiry!.Value.AddDays(-7)) // RefreshTokenExpiry = login + 7 days
                .FirstOrDefault();

            return new TenantListDto(
                t.Id,
                t.Name,
                t.Slug,
                t.Email,
                t.Phone,
                t.City,
                t.IsActive,
                t.EmailVerified,
                subStatus,
                t.SubscriptionEndDate,
                daysRemaining,
                t.Users.Count,
                t.Clients.Count,
                t.CreatedAt,
                lastLogin == default ? null : lastLogin
            );
        }).ToList();
    }
}
