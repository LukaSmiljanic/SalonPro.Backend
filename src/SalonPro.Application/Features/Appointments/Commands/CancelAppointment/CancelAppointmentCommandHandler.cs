using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Appointments.Commands.CancelAppointment;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<CancelAppointmentCommandHandler> _logger;

    public CancelAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<CancelAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Unit> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        if (appointment.Status == AppointmentStatus.Completed)
        {
            throw new ForbiddenAccessException("Cannot cancel a completed appointment.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = request.CancellationReason;
        appointment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Appointments.Update(appointment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send cancellation email (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                // Load related entities for email
                var client = await _unitOfWork.Clients.GetByIdAsync(appointment.ClientId, CancellationToken.None);
                if (client == null || string.IsNullOrWhiteSpace(client.Email)) return;

                var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(appointment.StaffMemberId, CancellationToken.None);
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(appointment.TenantId, CancellationToken.None);
                if (staffMember == null || tenant == null) return;

                var services = await _unitOfWork.AppointmentServices.Query()
                    .Where(a => a.AppointmentId == appointment.Id)
                    .Include(a => a.Service)
                    .ToListAsync(CancellationToken.None);

                var serviceNames = string.Join(", ", services.Select(s => s.Service.Name));

                var emailDto = new AppointmentEmailDto(
                    ClientName: client.FullName,
                    ClientEmail: client.Email,
                    SalonName: tenant.Name,
                    StaffName: staffMember.FullName,
                    StartTime: appointment.StartTime,
                    DurationMinutes: appointment.TotalDurationMinutes,
                    ServiceNames: serviceNames,
                    TotalPrice: appointment.TotalPrice,
                    Currency: tenant.Currency ?? "RSD",
                    SalonPhone: tenant.Phone,
                    SalonAddress: tenant.Address
                );

                await _emailService.SendAppointmentCancellationAsync(emailDto, request.CancellationReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email for appointment {AppointmentId}", appointment.Id);
            }
        }, cancellationToken);

        return Unit.Value;
    }
}
