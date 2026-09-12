using SalonPro.Application.Common;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Social;

internal static class SocialPlanGuard
{
    public static void EnsureAllowed(Tenant? tenant)
    {
        if (tenant == null)
            throw new ForbiddenAccessException("Kontekst salona nije postavljen.");

        if (!TenantPlanRules.CanUseSocialMarketing(tenant.Plan))
            throw new ForbiddenAccessException(
                "Marketing modul je dostupan u Pro paketu. Nadogradite pretplatu za pristup.");
    }
}
