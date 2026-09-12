using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Tenants.Commands.ActivateTenantAccess;
using SalonPro.Application.Features.Tenants.Commands.ProvisionDemoTenant;
using SalonPro.Application.Features.Tenants.Commands.UpdateTenantPlan;
using SalonPro.Application.Features.Tenants.DTOs;
using SalonPro.Application.Features.Tenants.Queries.GetTenants;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ApiControllerBase
{
    /// <summary>
    /// List all tenants with subscription status, user count, and last login info. SuperAdmin only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TenantListDto>), 200)]
    public async Task<IActionResult> GetTenants()
    {
        var result = await Mediator.Send(new GetTenantsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Provisions a 30-day demo tenant + admin user; sends welcome email with temporary password. SuperAdmin only.
    /// </summary>
    [HttpPost("demo")]
    [ProducesResponseType(typeof(ProvisionDemoTenantResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ProvisionDemo([FromBody] ProvisionDemoTenantCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Verifies tenant email and sets trial subscription (sales demo). SuperAdmin only.
    /// </summary>
    [HttpPost("{id:guid}/activate-access")]
    [ProducesResponseType(typeof(ActivateTenantAccessResult), 200)]
    public async Task<IActionResult> ActivateAccess([FromRoute] Guid id, [FromBody] ActivateTenantAccessRequest? body)
    {
        var result = await Mediator.Send(new ActivateTenantAccessCommand(id, body?.TrialDays ?? 30));
        return Ok(result);
    }

    public record ActivateTenantAccessRequest(int? TrialDays);

    public record UpdateTenantPlanRequest(string Plan);

    [HttpPut("{id:guid}/plan")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePlan([FromRoute] Guid id, [FromBody] UpdateTenantPlanRequest body)
    {
        await Mediator.Send(new UpdateTenantPlanCommand(id, body.Plan));
        return NoContent();
    }
}
