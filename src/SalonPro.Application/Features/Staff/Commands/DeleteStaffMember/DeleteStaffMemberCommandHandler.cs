using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Staff.Commands.DeleteStaffMember;

public class DeleteStaffMemberCommandHandler : IRequestHandler<DeleteStaffMemberCommand, DeleteStaffMemberResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteStaffMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteStaffMemberResult> Handle(DeleteStaffMemberCommand request, CancellationToken cancellationToken)
    {
        var staff = await _unitOfWork.StaffMembers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.Id);

        var now = DateTime.UtcNow;
        var futureAppointments = await _unitOfWork.Appointments.Query()
            .Where(a =>
                a.StaffMemberId == request.Id &&
                a.StartTime >= now &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Completed &&
                a.Status != AppointmentStatus.NoShow)
            .ToListAsync(cancellationToken);

        if (futureAppointments.Count > 0)
        {
            foreach (var appointment in futureAppointments)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.CancellationReason = "Termin je automatski otkazan jer zaposleni više nije aktivan.";
                appointment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Appointments.Update(appointment);
            }
        }

        // Check if staff member has any appointments (history)
        var hasAppointments = await _unitOfWork.Appointments
            .CountAsync(a => a.StaffMemberId == request.Id, cancellationToken) > 0;

        if (hasAppointments)
        {
            // Soft delete – deactivate so historical data is preserved
            staff.IsActive = false;
            staff.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.StaffMembers.Update(staff);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteStaffMemberResult(
                WasSoftDeleted: true,
                Message: futureAppointments.Count > 0
                    ? $"Zaposleni je deaktiviran. Automatski je otkazano {futureAppointments.Count} budućih termina, a istorija je sačuvana."
                    : "Zaposleni je deaktiviran jer ima istoriju termina. Podaci su sačuvani."
            );
        }

        // No history – safe to hard delete
        // Also remove any working hours
        var workingHours = await _unitOfWork.WorkingHours
            .FindAsync(wh => wh.StaffMemberId == request.Id, cancellationToken);

        foreach (var wh in workingHours)
        {
            _unitOfWork.WorkingHours.Remove(wh);
        }

        _unitOfWork.StaffMembers.Remove(staff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteStaffMemberResult(
            WasSoftDeleted: false,
            Message: "Zaposleni je uspešno obrisan."
        );
    }
}
