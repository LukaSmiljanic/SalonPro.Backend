namespace SalonPro.Domain.Enums;

public enum InsightType
{
    ScheduleGap,
    ClientReEngagement,
    RevenueChange,
    NoShowRisk,
    PeakHours,
    ServiceUpsell,
    VisitPattern,
    RebookingSuggestion,
    ChurnRisk,
    SpendingTrend,
    PreferredStaff,
    ServiceHistory
}

public enum InsightPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
