using MediatR;

namespace SalonPro.Application.Features.Staff.Commands.CreateStaffMember;

public record CreateStaffMemberCommand(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Title,
    int ColorIndex
) : IRequest<Guid>;
