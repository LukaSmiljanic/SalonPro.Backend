using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SalonPro.Domain.Enums;
using SalonPro.Infrastructure.Persistence;

namespace SalonPro.API.Middleware;

/// <summary>
/// Middleware that blocks API requests for tenants whose subscription has expired.
/// SuperAdmin users and auth endpoints are exempt.
/// </summary>
public class SubscriptionCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionCheckMiddleware> _logger;

    private static readonly string[] ExemptPrefixes =
    [
        "/api/auth/",
        "/api/public/",
        "/api/payments",
        "/api/subscriptions",
        "/api/tenants",
        "/swagger",
        "/health",
        "/favicon"
    ];

    public SubscriptionCheckMiddleware(RequestDelegate next, ILogger<SubscriptionCheckMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip exempt paths
        if (path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
            ExemptPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Only check authenticated users
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // SuperAdmin bypasses subscription checks
        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);
        if (roleClaim == nameof(UserRole.SuperAdmin))
        {
            await _next(context);
            return;
        }

        // Get tenant ID from claims
        var tenantClaim = context.User.FindFirstValue("tenant_id");
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            await _next(context);
            return;
        }

        // Check subscription status
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.SubscriptionEndDate, t.IsActive })
            .FirstOrDefaultAsync();

        if (tenant == null)
        {
            await _next(context);
            return;
        }

        var isSubscriptionActive = tenant.SubscriptionEndDate.HasValue &&
                                   tenant.SubscriptionEndDate.Value > DateTime.UtcNow;

        if (!isSubscriptionActive || !tenant.IsActive)
        {
            _logger.LogWarning(
                "Blocked request to {Path} — tenant {TenantId} subscription expired or inactive.",
                path, tenantId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Pretplata je istekla.",
                status = 403,
                detail = "Vaša pretplata je istekla. Kontaktirajte podršku za produženje."
            });
            return;
        }

        await _next(context);
    }
}
