using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Auth.Commands.Login;
using SalonPro.Application.Features.Auth.Commands.RefreshToken;
using SalonPro.Application.Features.Auth.Commands.Register;
using SalonPro.Application.Features.Auth.Commands.ForgotPassword;
using SalonPro.Application.Features.Auth.Commands.ResetPassword;
using SalonPro.Application.Features.Auth.Commands.VerifyEmail;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await Mediator.Send(new VerifyEmailCommand(token));
        if (result.Success)
            return Ok(new { message = result.Message });

        return BadRequest(new { message = result.Message });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await Mediator.Send(command);
        // Always return OK to prevent email enumeration
        return Ok(new { message = "Ako nalog postoji, poslaćemo vam email sa linkom za resetovanje lozinke." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        if (result.Success)
            return Ok(new { message = result.Message });

        return BadRequest(new { message = result.Message });
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { userId, email, role });
    }
}
