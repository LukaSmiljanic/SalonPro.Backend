using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Insights.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Insights.Queries.GetClientInsights;

public class GetClientInsightsQueryHandler : IRequestHandler<GetClientInsightsQuery, ClientInsightsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetClientInsightsQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<ClientInsightsDto> Handle(GetClientInsightsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required.");

        var now = DateTime.UtcNow;
        var insights = new List<InsightDto>();

        var clientAppointments = _unitOfWork.Appointments.Query()
            .Where(a => a.TenantId == tenantId && a.ClientId == request.ClientId);

        var completedAppointments = await clientAppointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .OrderBy(a => a.StartTime)
            .Select(a => new
            {
                a.StartTime,
                a.TotalPrice,
                a.StaffMemberId,
                StaffName = a.StaffMember.FirstName + " " + a.StaffMember.LastName
            })
            .ToListAsync(cancellationToken);

        var allAppointments = await clientAppointments
            .Select(a => new { a.Status, a.StartTime })
            .ToListAsync(cancellationToken);

        // ═══════════════════════════════════════════════════════════════
        // 1. VISIT PATTERN — average days between visits
        // ═══════════════════════════════════════════════════════════════
        double avgCycleDays = 0;
        DateTime? suggestedNextVisit = null;

        if (completedAppointments.Count >= 2)
        {
            var gaps = new List<double>();
            for (int i = 1; i < completedAppointments.Count; i++)
            {
                gaps.Add((completedAppointments[i].StartTime - completedAppointments[i - 1].StartTime).TotalDays);
            }
            avgCycleDays = Math.Round(gaps.Average(), 1);
            var lastVisit = completedAppointments.Last().StartTime;
            suggestedNextVisit = lastVisit.AddDays(avgCycleDays);

            insights.Add(new InsightDto(
                InsightType.VisitPattern,
                InsightPriority.Low,
                $"Dolazi svaka {Math.Round(avgCycleDays)} dana",
                $"Prosečan ciklus poseta je {avgCycleDays} dana. Poslednja poseta: {lastVisit:dd.MM.yyyy}.",
                "CalendarClock"
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. REBOOKING SUGGESTION — based on visit cycle
        // ═══════════════════════════════════════════════════════════════
        if (suggestedNextVisit.HasValue)
        {
            var daysUntil = (suggestedNextVisit.Value - now).TotalDays;
            var hasFutureBooking = allAppointments.Any(a =>
                a.StartTime > now && a.Status != AppointmentStatus.Cancelled);

            if (!hasFutureBooking)
            {
                var priority = daysUntil switch
                {
                    <= 0 => InsightPriority.Urgent,
                    <= 7 => InsightPriority.High,
                    _ => InsightPriority.Medium
                };

                var text = daysUntil <= 0
                    ? $"Termin je trebao biti zakazan pre {Math.Abs(Math.Round(daysUntil))} dana. Kontaktirajte klijenta."
                    : $"Na osnovu prosečnog ciklusa, sledeća poseta bi trebala biti oko {suggestedNextVisit.Value:dd.MM.yyyy}.";

                insights.Add(new InsightDto(
                    InsightType.RebookingSuggestion,
                    priority,
                    daysUntil <= 0 ? "Vreme za ponovo zakazivanje!" : "Predlog za zakazivanje",
                    text,
                    "CalendarPlus",
                    "Zakaži termin"
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. CHURN RISK — no visit for over 2x their average cycle
        // ═══════════════════════════════════════════════════════════════
        if (completedAppointments.Any())
        {
            var lastVisit = completedAppointments.Last().StartTime;
            var daysSinceLastVisit = (now - lastVisit).TotalDays;
            var threshold = avgCycleDays > 0 ? avgCycleDays * 2 : 60;

            if (daysSinceLastVisit > threshold)
            {
                insights.Add(new InsightDto(
                    InsightType.ChurnRisk,
                    daysSinceLastVisit > threshold * 1.5 ? InsightPriority.Urgent : InsightPriority.High,
                    "Rizik od gubitka klijenta",
                    $"Prošlo je {Math.Round(daysSinceLastVisit)} dana od poslednje posete (prosek: {Math.Round(avgCycleDays)} dana). Pošaljite personalizovanu ponudu.",
                    "UserX"
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 4. SPENDING TREND — compare recent vs. earlier average
        // ═══════════════════════════════════════════════════════════════
        decimal avgSpend = 0;
        if (completedAppointments.Count >= 2)
        {
            avgSpend = Math.Round(completedAppointments.Average(a => a.TotalPrice), 0);
            var half = completedAppointments.Count / 2;
            var recentAvg = completedAppointments.Skip(half).Average(a => a.TotalPrice);
            var earlierAvg = completedAppointments.Take(half).Average(a => a.TotalPrice);

            if (earlierAvg > 0)
            {
                var changePercent = Math.Round((recentAvg - earlierAvg) / earlierAvg * 100, 1);
                if (Math.Abs(changePercent) >= 15)
                {
                    var isUp = changePercent > 0;
                    insights.Add(new InsightDto(
                        InsightType.SpendingTrend,
                        InsightPriority.Medium,
                        isUp ? $"Potrošnja raste +{changePercent}%" : $"Potrošnja opada {changePercent}%",
                        isUp
                            ? $"Prosečna potrošnja se povećala za {changePercent}%. Klijent koristi više usluga."
                            : $"Prosečna potrošnja je opala za {Math.Abs(changePercent)}%. Razmislite o ponudi paketa.",
                        isUp ? "TrendingUp" : "TrendingDown"
                    ));
                }
            }
        }
        else if (completedAppointments.Count == 1)
        {
            avgSpend = completedAppointments.First().TotalPrice;
        }

        // ═══════════════════════════════════════════════════════════════
        // 5. PREFERRED STAFF — most visited staff member
        // ═══════════════════════════════════════════════════════════════
        string? preferredStaffName = null;
        if (completedAppointments.Count >= 2)
        {
            var staffGroups = completedAppointments
                .GroupBy(a => a.StaffMemberId)
                .OrderByDescending(g => g.Count())
                .First();

            preferredStaffName = staffGroups.First().StaffName;
            var pct = Math.Round((double)staffGroups.Count() / completedAppointments.Count * 100, 0);

            if (pct >= 60)
            {
                insights.Add(new InsightDto(
                    InsightType.PreferredStaff,
                    InsightPriority.Low,
                    $"Preferirani zaposleni: {preferredStaffName}",
                    $"Klijent zakazuje kod {preferredStaffName} u {pct}% slučajeva. Obratite pažnju na dostupnost.",
                    "UserCheck"
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 6. SERVICE HISTORY — top service
        // ═══════════════════════════════════════════════════════════════
        string? topService = null;
        var clientAppServices = await _unitOfWork.AppointmentServices.Query()
            .Where(aps => clientAppointments.Any(a => a.Id == aps.AppointmentId
                && a.Status == AppointmentStatus.Completed))
            .Include(aps => aps.Service)
            .GroupBy(aps => aps.Service.Name)
            .Select(g => new { ServiceName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);

        if (clientAppServices != null)
        {
            topService = clientAppServices.ServiceName;
            insights.Add(new InsightDto(
                InsightType.ServiceHistory,
                InsightPriority.Low,
                $"Omiljena usluga: {topService}",
                $"Klijent najčešće koristi \"{topService}\" ({clientAppServices.Count}x). Ponudite komplementarne usluge.",
                "Sparkles"
            ));
        }

        // Sort by priority descending
        insights.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return new ClientInsightsDto(
            insights,
            avgCycleDays,
            suggestedNextVisit,
            preferredStaffName,
            topService,
            avgSpend
        );
    }
}
