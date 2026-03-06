using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Appointments.Commands.CompletePastAppointments;

public class CompletePastAppointmentsCommandHandler : IRequestHandler<CompletePastAppointmentsCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompletePastAppointmentsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CompletePastAppointmentsCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var toComplete = await _unitOfWork.Appointments.Query()
            .Where(a =>
                a.EndTime < now &&
                a.Status != AppointmentStatus.Completed &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.NoShow)
            .ToListAsync(cancellationToken);

        foreach (var a in toComplete)
        {
            a.Status = AppointmentStatus.Completed;
            a.UpdatedAt = now;
            _unitOfWork.Appointments.Update(a);
        }

        if (toComplete.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return toComplete.Count;
    }
}
