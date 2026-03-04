using Microsoft.AspNetCore.Mvc;
using SalonPro.Application.Features.Staff.Commands.CreateStaffMember;
using SalonPro.Application.Features.Staff.DTOs;
using SalonPro.Application.Features.Staff.Queries.GetStaffMembers;
using SalonPro.Application.Features.Staff.Queries.GetStaffSchedule;

namespace SalonPro.API.Controllers;

[ApiController]
[Route("api/staff")]
public class StaffController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<StaffMemberDto>), 200)]
    public async Task<IActionResult> GetStaffMembers([FromQuery] bool includeInactive = false)
    {
        var result = await Mediator.Send(new GetStaffMembersQuery(includeInactive));
        return Ok(result);
    }

    [HttpGet("{staffMemberId:guid}/schedule")]
    [ProducesResponseType(typeof(StaffScheduleDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStaffSchedule(
        [FromRoute] Guid staffMemberId,
        [FromQuery] DateTime date)
    {
        var result = await Mediator.Send(new GetStaffScheduleQuery(staffMemberId, date));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateStaffMember([FromBody] CreateStaffMemberCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }
}
