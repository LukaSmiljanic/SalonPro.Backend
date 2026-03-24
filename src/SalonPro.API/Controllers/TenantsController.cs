using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
