using MediatR;
using SalonPro.Application.Features.Staff.DTOs;

namespace SalonPro.Application.Features.Staff.Queries.GetStaffSchedule;

public record GetStaffScheduleQuery(
    Guid StaffMemberId,
    DateTime Date
) : IRequest<StaffScheduleDto>;
