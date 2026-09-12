namespace SalonPro.Application.Common;

public static class TenantPlanRules
{
    public const string Basic = "Basic";
    public const string Standard = "Standard";
    public const string Pro = "Pro";
    /// <summary>Short-term sales demo (provisioned by SuperAdmin).</summary>
    public const string Demo = "Demo";

    public static string Normalize(string? plan)
    {
        if (string.Equals(plan, Demo, StringComparison.OrdinalIgnoreCase)) return Demo;
        if (string.Equals(plan, Standard, StringComparison.OrdinalIgnoreCase)) return Standard;
        if (string.Equals(plan, Pro, StringComparison.OrdinalIgnoreCase)) return Pro;
        return Basic;
    }

    public static bool CanUseOnlineBooking(string? plan)
    {
        var normalized = Normalize(plan);
        return normalized is Standard or Pro or Demo;
    }

    /// <summary>AI marketing / Instagram planner — Pro and Demo (dev preview).</summary>
    public static bool CanUseSocialMarketing(string? plan)
    {
        var normalized = Normalize(plan);
        return normalized is Pro or Demo;
    }

    public static int MaxStaffMembers(string? plan)
    {
        var normalized = Normalize(plan);
        return normalized switch
        {
            Demo => 5,
            Standard => 5,
            Pro => int.MaxValue,
            _ => 1,
        };
    }
}

