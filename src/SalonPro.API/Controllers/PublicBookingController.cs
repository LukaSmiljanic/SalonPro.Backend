using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Common;
using SalonPro.Application.Features.PublicBooking.Commands.CreatePublicBooking;
using SalonPro.Application.Features.PublicBooking.DTOs;
using SalonPro.Application.Features.PublicBooking.Queries.GetPublicBookingContext;
using SalonPro.Application.Features.Services.Queries.GetServices;
using SalonPro.Application.Features.Staff.Queries.GetStaffMembers;
using SalonPro.Domain.Interfaces;

namespace SalonPro.API.Controllers;

/// <summary>
/// Anonymous online booking for a salon, resolved by tenant <see cref="Domain.Entities.Tenant.Slug"/>.
/// The SPA lives on a separate origin; CORS + same API.
/// </summary>
[ApiController]
[Route("api/public/booking")]
[AllowAnonymous]
public class PublicBookingController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICurrentTenantService _currentTenant;

    public PublicBookingController(ISender mediator, ICurrentTenantService currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicBookingSalonDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSalon([FromRoute] string slug, CancellationToken cancellationToken)
    {
        var ctx = await _mediator.Send(new GetPublicBookingContextQuery(slug), cancellationToken);
        if (ctx == null)
            return NotFound();
        return Ok(ctx.Salon);
    }

    [HttpGet("{slug}/services")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetServices([FromRoute] string slug, CancellationToken cancellationToken)
    {
        var ctx = await _mediator.Send(new GetPublicBookingContextQuery(slug), cancellationToken);
        if (ctx == null)
            return NotFound();
        if (!TenantPlanRules.CanUseOnlineBooking(ctx.Plan))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                title = "Online zakazivanje nije dostupno.",
                detail = "Nadogradite paket salona da biste uključili online zakazivanje."
            });

        _currentTenant.SetTenant(ctx.TenantId);
        var services = await _mediator.Send(new GetServicesQuery(false), cancellationToken);
        return Ok(services);
    }

    [HttpGet("{slug}/staff")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStaff([FromRoute] string slug, CancellationToken cancellationToken)
    {
        var ctx = await _mediator.Send(new GetPublicBookingContextQuery(slug), cancellationToken);
        if (ctx == null)
            return NotFound();
        if (!TenantPlanRules.CanUseOnlineBooking(ctx.Plan))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                title = "Online zakazivanje nije dostupno.",
                detail = "Nadogradite paket salona da biste uključili online zakazivanje."
            });

        _currentTenant.SetTenant(ctx.TenantId);
        var staff = await _mediator.Send(new GetStaffMembersQuery(false), cancellationToken);
        var minimal = staff.Select(s => new { s.Id, s.FullName }).ToList();
        return Ok(minimal);
    }

    public record CreatePublicBookingRequest(
        string FirstName,
        string LastName,
        string Phone,
        string? Email,
        Guid StaffMemberId,
        List<Guid> ServiceIds,
        DateTime StartTime,
        string? Notes
    );

    [HttpPost("{slug}/appointments")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateBooking(
        [FromRoute] string slug,
        [FromBody] CreatePublicBookingRequest body,
        CancellationToken cancellationToken)
    {
        var ctx = await _mediator.Send(new GetPublicBookingContextQuery(slug), cancellationToken);
        if (ctx == null)
            return NotFound();
        if (!TenantPlanRules.CanUseOnlineBooking(ctx.Plan))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                title = "Online zakazivanje nije dostupno.",
                detail = "Nadogradite paket salona da biste uključili online zakazivanje."
            });

        _currentTenant.SetTenant(ctx.TenantId);

        var id = await _mediator.Send(
            new CreatePublicBookingCommand(
                body.FirstName,
                body.LastName,
                body.Phone,
                body.Email,
                body.StaffMemberId,
                body.ServiceIds,
                body.StartTime,
                body.Notes),
            cancellationToken);

        return Created($"/api/appointments/{id}", id);
    }
}
