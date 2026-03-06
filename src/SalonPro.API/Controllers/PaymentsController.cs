using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Payments.Commands.CreatePayment;
using SalonPro.Application.Features.Payments.Commands.DeletePayment;
using SalonPro.Application.Features.Payments.Commands.UpdatePaymentStatus;
using SalonPro.Application.Features.Payments.DTOs;
using SalonPro.Application.Features.Payments.Queries.GetPayments;
using SalonPro.Application.Features.Payments.Queries.GetPaymentSummary;
using SalonPro.Domain.Enums;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "SuperAdmin")]
public class PaymentsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentDto>), 200)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] PaymentStatus? status = null)
    {
        var result = await Mediator.Send(new GetPaymentsQuery(tenantId, year, month, status));
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(List<PaymentSummaryDto>), 200)]
    public async Task<IActionResult> GetPaymentSummary()
    {
        var result = await Mediator.Send(new GetPaymentSummaryQuery());
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetPayments), new { id }, id);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePaymentStatus(
        [FromRoute] Guid id,
        [FromBody] UpdatePaymentStatusCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch.");

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePayment([FromRoute] Guid id)
    {
        await Mediator.Send(new DeletePaymentCommand(id));
        return NoContent();
    }
}
