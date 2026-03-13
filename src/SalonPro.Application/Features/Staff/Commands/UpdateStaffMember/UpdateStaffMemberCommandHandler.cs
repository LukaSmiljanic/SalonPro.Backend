using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Staff.Commands.UpdateStaffMember;

public class UpdateStaffMemberCommandHandler : IRequestHandler<UpdateStaffMemberCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStaffMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateStaffMemberCommand request, CancellationToken cancellationToken)
    {
        var staff = await _unitOfWork.StaffMembers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.Id);

        staff.FirstName = request.FirstName;
        staff.LastName = request.LastName;
        staff.Email = request.Email;
        staff.Phone = request.Phone;
        staff.Specialization = request.Specialization;
        staff.IsActive = request.IsActive;
        staff.ColorIndex = request.ColorIndex;
        staff.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.StaffMembers.Update(staff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
