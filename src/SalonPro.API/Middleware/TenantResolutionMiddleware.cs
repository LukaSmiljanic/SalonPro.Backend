using System.Security.Claims;
using SalonPro.Domain.Interfaces;

namespace SalonPro.API.Middleware;

/// <summary>
/// Middleware that resolves the current tenant from the X-Tenant-Id request header
/// or from the authenticated user's JWT claims, then sets it on <see cref="ICurrentTenantService"/>.
/// Auth endpoints (login/register) are exempt from the tenant requirement.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    // Auth endpoints do not require a tenant ID
    private static readonly HashSet<string> _tenantExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register"
    };

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService currentTenantService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Swagger UI, health checks, and other infrastructure paths are exempt
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        Guid? tenantId = null;

        // 1. Try X-Tenant-Id header first
      if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
            && Guid.TryParse(tenantHeader.FirstOrDefault(), out var headerTenantId))
        {
            tenantId = headerTenantId;
            _logger.LogDebug("Tenant resolved from header: {TenantId}", tenantId);
        }

        // 2. Fall back to tenant_id claim from JWT
        if (tenantId == null && context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirstValue("tenant_id");
            if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var claimTenantId))
            {
                tenantId = claimTenantId;
                _logger.LogDebug("Tenant resolved from JWT claim: {TenantId}", tenantId);
            }
        }

        if (tenantId.HasValue)
        {
            currentTenantService.SetTenant(tenantId.Value);
            await _next(context);
            return;
        }

        // Auth-exempt paths proceed without a tenant ID
        if (_tenantExemptPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // All other authenticated endpoints require a tenant ID
        _logger.LogWarning("Request to {Path} missing required Tenant ID.", path);
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Tenant ID is required.",
            status = 400,
            detail = "Provide a valid tenant ID via the X-Tenant-Id header or ensure your JWT token contains a tenant_id claim."
        });
    }

    private static bool IsExemptPath(string path)
    {
        return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
    }
}
