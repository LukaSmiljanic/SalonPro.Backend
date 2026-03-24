using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Features.Clients.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Queries.GetClientById;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetClientByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<ClientDetailDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _unitOfWork.Clients.Query()
            .Include(c => c.Appointments)
                .ThenInclude(a => a.AppointmentServices)
                    .ThenInclude(aps => aps.Service)
            .Include(c => c.Appointments)
                .ThenInclude(a => a.StaffMember)
            .Include(c => c.ClientNotes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.Id);

        var completedAppointments = client.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.StartTime)
            .ToList();

        var totalVisits = completedAppointments.Count;
        var totalSpent = completedAppointments.Sum(a => a.TotalPrice);
        var lastVisitDate = completedAppointments.FirstOrDefault()?.StartTime;

        var visitHistory = completedAppointments.Select(a => new VisitHistoryDto(
            a.StartTime,
            string.Join(", ", a.AppointmentServices.Select(aps => aps.Service.Name)),
            a.StaffMember.FullName,
            a.TotalPrice
        )).ToList();

        var notes = client.ClientNotes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ClientNoteDto(n.Id, n.Content, n.CreatedAt, n.CreatedBy))
            .ToList();

        // Compute loyalty info using tenant config from DB
        var loyalty = await BuildLoyaltyFromConfig(totalVisits, cancellationToken);

        return new ClientDetailDto(
            client.Id,
            client.FirstName,
            client.LastName,
            client.FullName,
            client.Email,
            client.Phone ?? string.Empty,
            client.Notes,
            client.IsVip,
            client.Tags,
            totalVisits,
            totalSpent,
            lastVisitDate,
            visitHistory,
            notes,
            loyalty
        );
    }

    private async Task<ClientLoyaltyDto> BuildLoyaltyFromConfig(int totalVisits, CancellationToken cancellationToken)
    {
        // Try to load tenant-specific loyalty config from DB
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
                var tiers = configs.Select(c => (c.MinVisits, c.TierName, c.Benefit)).ToList();
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

        return (null, 0, null);
    }
}
