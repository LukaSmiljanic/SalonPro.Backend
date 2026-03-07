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

public record DashboardInsightsDto(
    List<InsightDto> Insights,
    int InactiveClientsCount,
    int ScheduleGapsCount,
    decimal WeekRevenueChangePercent
);

public record ClientInsightsDto(
    List<InsightDto> Insights,
    double AverageVisitCycleDays,
    DateTime? SuggestedNextVisit,
    string? PreferredStaffName,
    string? TopService,
    decimal AverageSpendPerVisit
);
