using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Queries.GetClientLoyalty;

public class GetClientLoyaltyQueryHandler : IRequestHandler<GetClientLoyaltyQuery, ClientLoyaltyDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetClientLoyaltyQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<ClientLoyaltyDto> Handle(GetClientLoyaltyQuery request, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Clients.Query()
            .AnyAsync(c => c.Id == request.ClientId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(Client), request.ClientId);

        var totalVisits = await _unitOfWork.Appointments.Query()
            .CountAsync(a => a.ClientId == request.ClientId && a.Status == AppointmentStatus.Completed, cancellationToken);

        // Try to load tenant-specific loyalty config from DB
        List<(int MinVisits, string TierName, string Benefit)> tiers;

        var tenantId = _currentTenantService.TenantId;
        if (tenantId.HasValue)
        {
            var configs = await _unitOfWork.LoyaltyConfigs.Query()
                .Where(lc => lc.TenantId == tenantId.Value)
                .OrderBy(lc => lc.MinVisits)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (configs.Count > 0)
            {
                tiers = configs.Select(c => (c.MinVisits, c.TierName, c.Benefit)).ToList();
                return BuildLoyaltyDto(totalVisits, tiers);
            }
        }

        // Fallback to hardcoded defaults
        var (tier, benefit) = GetLoyaltyTier(totalVisits);
        var (nextMilestone, visitsUntil, nextBenefit) = GetNextMilestone(totalVisits);

        return new ClientLoyaltyDto(
            totalVisits,
            tier.ToString(),
            benefit,
            nextMilestone,
            visitsUntil,
            nextBenefit
        );
    }

    private static ClientLoyaltyDto BuildLoyaltyDto(
        int totalVisits,
        List<(int MinVisits, string TierName, string Benefit)> tiers)
    {
        // Find current tier (highest tier whose MinVisits <= totalVisits)
        string currentTierName = "None";
        string? currentBenefit = null;

        foreach (var tier in tiers)
        {
            if (totalVisits >= tier.MinVisits)
            {
                currentTierName = tier.TierName;
                currentBenefit = tier.Benefit;
            }
        }

        // Find next milestone
        int? nextMilestone = null;
        int visitsUntil = 0;
        string? nextBenefit = null;

        foreach (var tier in tiers)
        {
            if (totalVisits < tier.MinVisits)
            {
                nextMilestone = tier.MinVisits;
                visitsUntil = tier.MinVisits - totalVisits;
                nextBenefit = tier.Benefit;
                break;
            }
        }

        return new ClientLoyaltyDto(
            totalVisits,
            currentTierName,
            currentBenefit,
            nextMilestone,
            visitsUntil,
            nextBenefit
        );
    }

    private static (LoyaltyTier Tier, string? Benefit) GetLoyaltyTier(int visits)
    {
        if (visits >= 100) return (LoyaltyTier.Platinum, "Besplatna usluga + poklon");
        if (visits >= 50) return (LoyaltyTier.Gold, "Besplatna usluga");
        if (visits >= 25) return (LoyaltyTier.Silver, "30% popusta");
        if (visits >= 10) return (LoyaltyTier.Bronze, "20% popusta");
        return (LoyaltyTier.None, null);
    }

    private static (int? NextMilestone, int VisitsUntil, string? NextBenefit) GetNextMilestone(int visits)
    {
        int[] milestones = [10, 25, 50, 100];
        string[] benefits = ["20% popusta", "30% popusta", "Besplatna usluga", "Besplatna usluga + poklon"];

        for (int i = 0; i < milestones.Length; i++)
        {
            if (visits < milestones[i])
            {
                return (milestones[i], milestones[i] - visits, benefits[i]);
            }
        }

        // Already at max tier
        return (null, 0, null);
    }
}
