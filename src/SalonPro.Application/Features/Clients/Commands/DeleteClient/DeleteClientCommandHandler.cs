using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Clients.Commands.DeleteClient;

public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClientCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.Id);

        var now = DateTime.UtcNow;
        var futureAppointments = await _unitOfWork.Appointments.Query()
            .Where(a =>
                a.ClientId == request.Id &&
                a.StartTime >= now &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Completed &&
                a.Status != AppointmentStatus.NoShow)
            .ToListAsync(cancellationToken);

        foreach (var appointment in futureAppointments)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = "Termin je automatski otkazan jer je klijent deaktiviran.";
            appointment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Appointments.Update(appointment);
        }

        client.IsActive = false;
        client.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Clients.Update(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
