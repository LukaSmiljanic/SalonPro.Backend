using MediatR;

namespace SalonPro.Application.Features.Staff.Commands.UpdateStaffMember;

public record UpdateStaffMemberCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Specialization,
    bool IsActive,
    int ColorIndex
) : IRequest<Unit>;
