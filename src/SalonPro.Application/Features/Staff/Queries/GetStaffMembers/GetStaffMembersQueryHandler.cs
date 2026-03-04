using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Staff.DTOs;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Staff.Queries.GetStaffMembers;

public class GetStaffMembersQueryHandler : IRequestHandler<GetStaffMembersQuery, List<StaffMemberDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStaffMembersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<StaffMemberDto>> Handle(GetStaffMembersQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _unitOfWork.StaffMembers.Query()
            .Include(s => s.Appointments.Where(a =>
                a.StartTime >= today &&
                a.StartTime < tomorrow &&
                a.Status != AppointmentStatus.Cancelled))
            .AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var staffMembers = await query
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync(cancellationToken);

        return staffMembers.Select(s => new StaffMemberDto(
            s.Id,
            s.FullName,
            s.Specialization,
            s.Email,
            s.Phone,
            s.IsActive,
            s.Appointments.Count
        )).ToList();
    }
}
