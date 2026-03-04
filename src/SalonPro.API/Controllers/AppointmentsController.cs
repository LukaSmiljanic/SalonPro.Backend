using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Appointments.Commands.CancelAppointment;
using SalonPro.Application.Features.Appointments.Commands.CompleteAppointment;
using SalonPro.Application.Features.Appointments.Commands.CreateAppointment;
using SalonPro.Application.Features.Appointments.Commands.UpdateAppointment;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Application.Features.Appointments.Queries.GetAppointmentById;
using SalonPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;
using SalonPro.Application.Features.Appointments.Queries.GetAppointmentsByStaff;
using SalonPro.Application.Features.Appointments.Queries.GetWeeklyCalendar;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ApiControllerBase
{
    [HttpGet("by-date")]
    [ProducesResponseType(typeof(List<AppointmentDto>), 200)]
    public async Task<IActionResult> GetByDate(
        [FromQuery] DateTime date,
        [FromQuery] Guid? staffMemberId = null)
    {
        var result = await Mediator.Send(new GetAppointmentsByDateQuery(date, staffMemberId));
        return Ok(result);
    }

    [HttpGet("by-staff/{staffMemberId:guid}")]
    [ProducesResponseType(typeof(List<AppointmentDto>), 200)]
    public async Task<IActionResult> GetByStaff(
        [FromRoute] Guid staffMemberId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await Mediator.Send(new GetAppointmentsByStaffQuery(staffMemberId, startDate, endDate));
        return Ok(result);
    }

    [HttpGet("weekly-calendar")]
    [ProducesResponseType(typeof(WeeklyCalendarDto), 200)]
    public async Task<IActionResult> GetWeeklyCalendar(
        [FromQuery] DateTime weekStartDate,
        [FromQuery] Guid? staffMemberId = null)
    {
        var result = await Mediator.Send(new GetWeeklyCalendarQuery(weekStartDate, staffMemberId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAppointmentByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAppointment(
        [FromRoute] Guid id,
        [FromBody] UpdateAppointmentCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch.");

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CancelAppointment(
        [FromRoute] Guid id,
        [FromBody] CancelAppointmentCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch.");

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CompleteAppointment([FromRoute] Guid id)
    {
        await Mediator.Send(new CompleteAppointmentCommand(id));
        return NoContent();
    }
}
