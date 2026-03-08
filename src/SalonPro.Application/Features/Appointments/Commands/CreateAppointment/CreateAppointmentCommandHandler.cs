using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;
using AppointmentServiceEntity = SalonPro.Domain.Entities.AppointmentService;

namespace SalonPro.Application.Features.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;

    public CreateAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IEmailService emailService,
        ILogger<CreateAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Tenant context is not set.");

        var client = await _unitOfWork.Clients.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.ClientId);

        var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(request.StaffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.StaffMemberId);

        var services = await _unitOfWork.Services.Query()
            .Where(s => request.ServiceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != request.ServiceIds.Count())
        {
            throw new NotFoundException("One or more services were not found or are inactive.");
        }

        var totalDuration = services.Sum(s => s.DurationMinutes);
        var totalPrice = services.Sum(s => s.Price);
        var endTime = request.StartTime.AddMinutes(totalDuration);

        var appointment = new Appointment
        {
            TenantId = tenantId,
            ClientId = request.ClientId,
            StaffMemberId = request.StaffMemberId,
            StartTime = request.StartTime,
            EndTime = endTime,
            Status = AppointmentStatus.Scheduled,
            TotalPrice = totalPrice,
            TotalDurationMinutes = totalDuration,
            Notes = request.Notes
        };

        await _unitOfWork.Appointments.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var service in services)
        {
            var appointmentService = new AppointmentServiceEntity
            {
                AppointmentId = appointment.Id,
                ServiceId = service.Id,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes
            };
            await _unitOfWork.AppointmentServices.AddAsync(appointmentService, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send confirmation email (fire-and-forget, don't block the response)
        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(client.Email)) return;

                var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, CancellationToken.None);
                if (tenant == null) return;

                var emailDto = new AppointmentEmailDto(
                    ClientName: client.FullName,
                    ClientEmail: client.Email,
                    SalonName: tenant.Name,
                    StaffName: staffMember.FullName,
                    StartTime: appointment.StartTime,
                    DurationMinutes: appointment.TotalDurationMinutes,
                    ServiceNames: string.Join(", ", services.Select(s => s.Name)),
                    TotalPrice: appointment.TotalPrice,
                    Currency: tenant.Currency ?? "RSD",
                    SalonPhone: tenant.Phone,
                    SalonAddress: tenant.Address
                );

                await _emailService.SendAppointmentConfirmationAsync(emailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment confirmation email for appointment {AppointmentId}", appointment.Id);
            }
        }, cancellationToken);

        return appointment.Id;
    }
}
