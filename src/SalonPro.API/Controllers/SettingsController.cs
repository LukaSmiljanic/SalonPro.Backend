using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Settings.Commands.UpdateLoyaltyConfig;
using SalonPro.Application.Features.Settings.Commands.UpdateWorkingHours;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Application.Features.Settings.Queries.GetLoyaltyConfig;
using SalonPro.Application.Features.Settings.Queries.GetWorkingHours;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ApiControllerBase
{
    [HttpGet("working-hours")]
    [ProducesResponseType(typeof(List<WorkingHoursDto>), 200)]
    public async Task<IActionResult> GetWorkingHours()
    {
        var result = await Mediator.Send(new GetWorkingHoursQuery());
        return Ok(result);
    }

    [HttpPut("working-hours")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateWorkingHours([FromBody] List<WorkingHourItem> items)
    {
        await Mediator.Send(new UpdateWorkingHoursCommand(items));
        return NoContent();
    }

    [HttpGet("loyalty-tiers")]
    [ProducesResponseType(typeof(List<LoyaltyConfigDto>), 200)]
    public async Task<IActionResult> GetLoyaltyTiers()
    {
        var result = await Mediator.Send(new GetLoyaltyConfigQuery());
        return Ok(result);
    }

    [HttpPut("loyalty-tiers")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateLoyaltyTiers([FromBody] List<LoyaltyTierItem> tiers)
    {
        await Mediator.Send(new UpdateLoyaltyConfigCommand(tiers));
        return NoContent();
    }
}
