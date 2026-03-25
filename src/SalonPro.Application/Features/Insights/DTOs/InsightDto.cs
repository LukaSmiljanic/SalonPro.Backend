using SalonPro.Domain.Enums;

namespace SalonPro.Application.Features.Insights.DTOs;

public record InsightDto(
    InsightType Type,
    InsightPriority Priority,
    string Title,
    string Description,
    string Icon,
    string? ActionLabel = null,
    string? ActionData = null
);

public record InactiveClientDto(
    Guid Id,
    string FullName,
    DateTime? LastVisit
);

public record DashboardInsightsDto(
    List<InsightDto> Insights,
    int InactiveClientsCount,
    int ScheduleGapsCount,
    decimal WeekRevenueChangePercent,
    List<InactiveClientDto> InactiveClients
);

public record ClientInsightsDto(
    List<InsightDto> Insights,
    double AverageVisitCycleDays,
    DateTime? SuggestedNextVisit,
    string? PreferredStaffName,
    string? TopService,
    decimal AverageSpendPerVisit
);
