using MediatR;
using SalonPro.Application.Features.Staff.DTOs;

namespace SalonPro.Application.Features.Staff.Queries.GetStaffMembers;

public record GetStaffMembersQuery(bool IncludeInactive = false) : IRequest<List<StaffMemberDto>>;
