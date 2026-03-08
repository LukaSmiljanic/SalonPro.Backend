using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Subscriptions.Commands.ExtendSubscription;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : ApiControllerBase
{
    /// <summary>
    /// Extend a tenant's subscription by a number of days. SuperAdmin only.
    /// </summary>
    [HttpPost("extend")]
    public async Task<IActionResult> ExtendSubscription([FromBody] ExtendSubscriptionRequest request)
    {
        var result = await Mediator.Send(new ExtendSubscriptionCommand(request.TenantId, request.Days));

        if (result.Success)
            return Ok(new { message = result.Message, newEndDate = result.NewEndDate });

        return BadRequest(new { message = result.Message });
    }

    /// <summary>
    /// Get subscription status for a specific tenant. SuperAdmin only.
    /// </summary>
    [HttpGet("{tenantId:guid}/status")]
    public async Task<IActionResult> GetSubscriptionStatus(Guid tenantId)
    {
        var tenant = await Mediator.Send(new GetSubscriptionStatusQuery(tenantId));
        return Ok(tenant);
    }
}

public record ExtendSubscriptionRequest(Guid TenantId, int Days);
