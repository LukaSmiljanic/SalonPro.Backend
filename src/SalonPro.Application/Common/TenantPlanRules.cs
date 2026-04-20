namespace SalonPro.Application.Common;

public static class TenantPlanRules
{
    public const string Basic = "Basic";
    public const string Standard = "Standard";
    public const string Pro = "Pro";

    public static string Normalize(string? plan)
    {
        if (string.Equals(plan, Standard, StringComparison.OrdinalIgnoreCase)) return Standard;
        if (string.Equals(plan, Pro, StringComparison.OrdinalIgnoreCase)) return Pro;
        return Basic;
    }

    public static bool CanUseOnlineBooking(string? plan)
    {
        var normalized = Normalize(plan);
        return normalized is Standard or Pro;
    }

    public static int MaxStaffMembers(string? plan)
    {
        var normalized = Normalize(plan);
        return normalized switch
        {
            Standard => 5,
            Pro => int.MaxValue,
            _ => 1,
        };
    }
}

