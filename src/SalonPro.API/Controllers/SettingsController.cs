using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Settings.Commands.UpdateLoyaltyConfig;
using SalonPro.Application.Features.Settings.Commands.UpdateTenantBranding;
using SalonPro.Application.Features.Settings.Commands.UpdateWorkingHours;
using SalonPro.Application.Features.Settings.Commands.UploadTenantLogo;
using SalonPro.Application.Features.Settings.DTOs;
using SalonPro.Application.Features.Settings.Queries.GetLoyaltyConfig;
using SalonPro.Application.Features.Settings.Queries.GetTenantBranding;
using SalonPro.Application.Features.Settings.Queries.GetWorkingHours;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ApiControllerBase
{
    [HttpGet("working-hours")]
    [ProducesResponseType(typeof(List<TenantWorkingHoursDto>), 200)]
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

    [HttpGet("branding")]
    [ProducesResponseType(typeof(TenantBrandingDto), 200)]
    public async Task<IActionResult> GetBranding()
    {
        var result = await Mediator.Send(new GetTenantBrandingQuery());
        return Ok(result);
    }

    [HttpPut("branding")]
    [ProducesResponseType(typeof(TenantBrandingDto), 200)]
    public async Task<IActionResult> UpdateBranding([FromBody] UpdateTenantBrandingRequest body)
    {
        var result = await Mediator.Send(new UpdateTenantBrandingCommand(
            body.PrimaryColor,
            body.AccentColor,
            body.OnlineBookingEnabled));
        return Ok(result);
    }

    [HttpPost("branding/logo")]
    [ProducesResponseType(typeof(TenantBrandingDto), 200)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { detail = "Izaberite logo fajl." });

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);

        var result = await Mediator.Send(new UploadTenantLogoCommand(
            file.FileName,
            ms.ToArray(),
            file.ContentType ?? "image/png"), cancellationToken);

        return Ok(result);
    }
}
