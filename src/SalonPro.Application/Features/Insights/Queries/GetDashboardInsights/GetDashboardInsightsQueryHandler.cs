using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Insights.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Insights.Queries.GetDashboardInsights;

public class GetDashboardInsightsQueryHandler : IRequestHandler<GetDashboardInsightsQuery, DashboardInsightsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;

    public GetDashboardInsightsQueryHandler(IUnitOfWork unitOfWork, ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
    }

    public async Task<DashboardInsightsDto> Handle(GetDashboardInsightsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var now = DateTime.UtcNow;
        var today = now.Date;
        var insights = new List<InsightDto>();

        var appointments = _unitOfWork.Appointments.Query().Where(a => a.TenantId == tenantId);
        var clients = _unitOfWork.Clients.Query().Where(c => c.TenantId == tenantId);

        // ═══════════════════════════════════════════════════════════════
        // 1. SCHEDULE GAP DETECTION — find empty slots in today's calendar
        // ═══════════════════════════════════════════════════════════════
        var todayAppointments = await appointments
            .Where(a => a.StartTime.Date == today && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(cancellationToken);

        var gapCount = 0;
        if (todayAppointments.Count > 1)
        {
            for (int i = 0; i < todayAppointments.Count - 1; i++)
            {
                var gap = todayAppointments[i + 1].StartTime - todayAppointments[i].EndTime;
                if (gap.TotalMinutes >= 60) gapCount++;
            }
        }

        if (gapCount > 0)
        {
            insights.Add(new InsightDto(
                InsightType.ScheduleGap,
                gapCount >= 3 ? InsightPriority.High : InsightPriority.Medium,
                $"{gapCount} prazan termin danas",
                gapCount == 1
                    ? "Imate 1 prazan period od 60+ minuta u današnjem rasporedu. Razmislite o popunjavanju."
                    : $"Imate {gapCount} prazna perioda od 60+ minuta. Razmislite o ponudi popusta za popunjavanje.",
                "CalendarClock",
                "Pogledaj raspored"
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. CLIENT RE-ENGAGEMENT — clients inactive for 30+ days
        //    A client is "active" if they had a Completed appointment in the
        //    last 30 days OR have any upcoming (Scheduled/Confirmed) appointment.
        // ═══════════════════════════════════════════════════════════════
        var thirtyDaysAgo = today.AddDays(-30);

        var recentlyCompletedClientIds = await appointments
            .Where(a => a.StartTime >= thirtyDaysAgo && a.Status == AppointmentStatus.Completed)
            .Select(a => a.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var upcomingClientIds2 = await appointments
            .Where(a => a.StartTime >= now
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .Select(a => a.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var activeClientIds = recentlyCompletedClientIds
            .Union(upcomingClientIds2)
            .Distinct()
            .ToList();

        var totalClientCount = await clients.CountAsync(cancellationToken);
        var inactiveCount = totalClientCount - activeClientIds.Count;
        var inactiveClientSample = inactiveCount > 0
            ? await clients
                .Where(c => !activeClientIds.Contains(c.Id))
                .Select(c => new { c.Id, c.FirstName, c.LastName })
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Fetch inactive client details (name + last visit)
        var inactiveClients = new List<InactiveClientDto>();
        if (inactiveCount > 0)
        {
            var inactiveClientEntities = await clients
                .Where(c => !activeClientIds.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    LastVisit = appointments
                        .Where(a => a.ClientId == c.Id && a.Status == AppointmentStatus.Completed)
                        .OrderByDescending(a => a.StartTime)
                        .Select(a => (DateTime?)a.StartTime)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            inactiveClients = inactiveClientEntities
                .Select(c => new InactiveClientDto(c.Id, $"{c.FirstName} {c.LastName}", c.LastVisit))
                .OrderBy(c => c.LastVisit ?? DateTime.MinValue) // longest inactive first
                .ToList();

            var priority = inactiveCount switch
            {
                > 10 => InsightPriority.Urgent,
                > 5 => InsightPriority.High,
                _ => InsightPriority.Medium
            };

            // Build description with client names (max 3 shown)
            var namesList = inactiveClients.Take(3).Select(c => c.FullName).ToList();
            var namesText = string.Join(", ", namesList);
            if (inactiveCount > 3)
                namesText += $" i još {inactiveCount - 3}";

            insights.Add(new InsightDto(
                InsightType.ClientReEngagement,
                priority,
                $"{inactiveCount} neaktivnih klijenata",
                $"{namesText} — bez termina poslednjih 30 dana. Pošaljite im podsetnik ili ponudu.",
                "UserX",
                "Pogledaj klijente",
                inactiveClientSample?.Id
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. REVENUE ANALYSIS — week-over-week change
        // ═══════════════════════════════════════════════════════════════
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek + 1); // Monday
        if (today.DayOfWeek == DayOfWeek.Sunday) thisWeekStart = today.AddDays(-6);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var lastWeekEnd = thisWeekStart;

        var thisWeekRevenue = await appointments
            .Where(a => a.StartTime >= thisWeekStart && a.StartTime < today.AddDays(1)
                && a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        var lastWeekRevenue = await appointments
            .Where(a => a.StartTime >= lastWeekStart && a.StartTime < lastWeekEnd
                && a.Status == AppointmentStatus.Completed)
            .SumAsync(a => (decimal?)a.TotalPrice, cancellationToken) ?? 0m;

        var weekChangePercent = lastWeekRevenue == 0
            ? (thisWeekRevenue > 0 ? 100m : 0m)
            : Math.Round((thisWeekRevenue - lastWeekRevenue) / lastWeekRevenue * 100, 1);

        if (weekChangePercent != 0)
        {
            var isPositive = weekChangePercent > 0;
            insights.Add(new InsightDto(
                InsightType.RevenueChange,
                Math.Abs(weekChangePercent) > 20 ? InsightPriority.High : InsightPriority.Medium,
                isPositive ? $"Prihod raste +{weekChangePercent}%" : $"Prihod pao {weekChangePercent}%",
                isPositive
                    ? $"Prihod ove nedelje je {weekChangePercent}% veći u odnosu na prošlu. Odlično!"
                    : $"Prihod ove nedelje je {Math.Abs(weekChangePercent)}% manji. Razmislite o promocijama.",
                isPositive ? "TrendingUp" : "TrendingDown"
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // 4. NO-SHOW RISK — clients with history of no-shows
        // ═══════════════════════════════════════════════════════════════
        var upcomingAppointments = await appointments
            .Where(a => a.StartTime >= now && a.StartTime <= today.AddDays(3)
                && a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.ClientId, a.Client.FirstName, a.Client.LastName })
            .ToListAsync(cancellationToken);

        var upcomingClientIds = upcomingAppointments.Select(a => a.ClientId).Distinct().ToList();

        if (upcomingClientIds.Any())
        {
            var noShowCounts = await appointments
                .Where(a => upcomingClientIds.Contains(a.ClientId) && a.Status == AppointmentStatus.NoShow)
                .GroupBy(a => a.ClientId)
                .Select(g => new { ClientId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 1)
                .ToListAsync(cancellationToken);

            if (noShowCounts.Any())
            {
                var riskCount = noShowCounts.Count;
                var riskClient = upcomingAppointments.FirstOrDefault(a => a.ClientId == noShowCounts.First().ClientId);
                var riskName = riskClient != null ? $"{riskClient.FirstName} {riskClient.LastName}" : "Klijent";

                insights.Add(new InsightDto(
                    InsightType.NoShowRisk,
                    InsightPriority.High,
                    riskCount == 1 ? $"Rizik od nedolaska: {riskName}" : $"{riskCount} klijenata sa rizikom od nedolaska",
                    riskCount == 1
                        ? $"{riskName} ima istoriju nedolazaka. Pošaljite podsetnik za predstojeći termin."
                        : $"{riskCount} klijenata sa predstojećim terminima ima istoriju nedolazaka. Pošaljite podsetnik.",
                    "AlertTriangle",
                    "Pogledaj klijenta",
                    riskClient?.ClientId
                ));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 5. PEAK HOURS — identify busiest hours this week
        // ═══════════════════════════════════════════════════════════════
        var weekAppointments = await appointments
            .Where(a => a.StartTime >= thisWeekStart && a.StartTime < today.AddDays(1)
                && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StartTime.Hour)
            .ToListAsync(cancellationToken);

        if (weekAppointments.Count >= 3)
        {
            var peakHour = weekAppointments
                .GroupBy(h => h)
                .OrderByDescending(g => g.Count())
                .First();

            insights.Add(new InsightDto(
                InsightType.PeakHours,
                InsightPriority.Low,
                $"Najzaposlenije vreme: {peakHour.Key}:00",
                $"Ove nedelje, {peakHour.Key}:00h je najtraženije vreme sa {peakHour.Count()} termina. Razmislite o dodatnom osoblju.",
                "Clock"
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // 6. SERVICE UPSELL — top service that can be paired
        // ═══════════════════════════════════════════════════════════════
        var recentServices = await _unitOfWork.AppointmentServices.Query()
            .Where(aps => appointments.Any(a => a.Id == aps.AppointmentId
                && a.StartTime >= thirtyDaysAgo
                && a.Status == AppointmentStatus.Completed))
            .Include(aps => aps.Service)
            .GroupBy(aps => aps.Service.Name)
            .Select(g => new { ServiceName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentServices != null)
        {
            insights.Add(new InsightDto(
                InsightType.ServiceUpsell,
                InsightPriority.Low,
                $"Najpopularnija usluga: {recentServices.ServiceName}",
                $"\"{recentServices.ServiceName}\" je naručena {recentServices.Count}x u poslednjih 30 dana. Ponudite kombinovani paket sa komplementarnom uslugom.",
                "Sparkles"
            ));
        }

        // Sort by priority descending
        insights.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return new DashboardInsightsDto(
            insights,
            inactiveCount,
            gapCount,
            weekChangePercent,
            inactiveClients
        );
    }
}
