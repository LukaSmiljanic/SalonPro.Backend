using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Interfaces;
using SalonPro.Application.Features.Social.Commands.DisconnectInstagram;
using SalonPro.Application.Features.Social.Queries.GetInstagramConnectUrl;
using SalonPro.Application.Features.Social.Queries.GetInstagramSetupDiagnostics;
using SalonPro.Application.Features.Social.Queries.GetInstagramStatus;

namespace SalonPro.API.Controllers;

[Route("api/social/instagram")]
public class SocialInstagramController : ApiControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(InstagramConnectionStatus), 200)]
    public async Task<IActionResult> GetStatus()
    {
        var result = await Mediator.Send(new GetInstagramStatusQuery());
        return Ok(result);
    }

    [HttpGet("setup-diagnostics")]
    [ProducesResponseType(typeof(InstagramSetupDiagnosticsDto), 200)]
    public async Task<IActionResult> GetSetupDiagnostics()
    {
        var result = await Mediator.Send(new GetInstagramSetupDiagnosticsQuery());
        return Ok(result);
    }

    [HttpGet("connect-url")]
    [ProducesResponseType(typeof(InstagramConnectUrl), 200)]
    public async Task<IActionResult> GetConnectUrl()
    {
        var result = await Mediator.Send(new GetInstagramConnectUrlQuery());
        return Ok(result);
    }

    [HttpDelete("disconnect")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Disconnect()
    {
        await Mediator.Send(new DisconnectInstagramCommand());
        return NoContent();
    }

    /// <summary>Meta OAuth redirect — no JWT; tenant id is in signed state.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromServices] IConfiguration configuration)
    {
        var frontend = configuration["AppSettings:FrontendUrl"]?.TrimEnd('/')
            ?? "https://salonpro.netlify.app";

        if (!string.IsNullOrEmpty(error))
            return MarketingRedirect(frontend, "error", error);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return MarketingRedirect(frontend, "error", "missing_code");

        try
        {
            var oauthState = HttpContext.RequestServices.GetRequiredService<IOAuthStateService>();
            var queue = HttpContext.RequestServices.GetRequiredService<IInstagramOAuthQueue>();
            var tenantId = oauthState.ParseState(state);
            await queue.EnqueueAsync(tenantId, code);
            return MarketingRedirect(frontend, "connecting");
        }
        catch (Exception ex)
        {
            return MarketingRedirect(frontend, "error", ex.Message);
        }
    }

    private static ContentResult MarketingRedirect(string frontend, string status, string? reason = null)
    {
        var url = $"{frontend}/marketing?instagram={status}";
        if (!string.IsNullOrEmpty(reason))
            url += $"&reason={Uri.EscapeDataString(reason)}";

        var html = $"""
            <!DOCTYPE html>
            <html lang="sr">
            <head>
              <meta charset="utf-8" />
              <meta http-equiv="refresh" content="0;url={url}" />
              <title>SalonPro — Instagram</title>
              <script>location.replace({System.Text.Json.JsonSerializer.Serialize(url)});</script>
            </head>
            <body>
              <p>Preusmeravanje na SalonPro…</p>
              <p><a href="{url}">Klikni ovde ako se ništa ne desi.</a></p>
            </body>
            </html>
            """;

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8",
            StatusCode = StatusCodes.Status200OK,
        };
    }

    [HttpGet("oauth-result")]
    [ProducesResponseType(typeof(InstagramOAuthJobResult), 200)]
    public IActionResult GetOAuthResult([FromServices] IInstagramOAuthQueue oauthQueue)
    {
        var tenantId = HttpContext.RequestServices.GetRequiredService<ICurrentTenantService>().TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");
        var result = oauthQueue.GetResult(tenantId);
        return Ok(result ?? new InstagramOAuthJobResult(InstagramOAuthJobStatus.Pending));
    }

    /// <summary>Called from Netlify after Meta redirects to the SPA (avoids broken API callback URL).</summary>
    [HttpPost("complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteOAuth([FromBody] InstagramOAuthCompleteRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.State))
            return BadRequest(new { message = "Nedostaje OAuth kod ili state." });

        try
        {
            var oauthState = HttpContext.RequestServices.GetRequiredService<IOAuthStateService>();
            var queue = HttpContext.RequestServices.GetRequiredService<IInstagramOAuthQueue>();
            var tenantId = oauthState.ParseState(body.State);
            await queue.EnqueueAsync(tenantId, body.Code);
            return Ok(new { status = "connecting" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record InstagramOAuthCompleteRequest(string Code, string State);
