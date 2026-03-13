using MediatR;

namespace SalonPro.Application.Features.Staff.Commands.DeleteStaffMember;

public record DeleteStaffMemberCommand(Guid Id) : IRequest<DeleteStaffMemberResult>;

public record DeleteStaffMemberResult(
    bool WasSoftDeleted,
    string Message
);
