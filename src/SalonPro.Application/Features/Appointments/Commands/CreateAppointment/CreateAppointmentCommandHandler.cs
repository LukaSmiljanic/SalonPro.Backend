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
    private readonly ISmsService _smsService;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;

    public CreateAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<CreateAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new InvalidOperationException("Kontekst salona nije postavljen.");

        var client = await _unitOfWork.Clients.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.ClientId);

        var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(request.StaffMemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(StaffMember), request.StaffMemberId);

        var services = await _unitOfWork.Services.Query()
            .Where(s => request.ServiceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != request.ServiceIds.Count())
        {
            throw new NotFoundException("Jedna ili više usluga nisu pronađene ili su neaktivne.");
        }

        // Validate against tenant working hours
        var appointmentDayOfWeek = request.StartTime.DayOfWeek;
        var tenantWorkingHours = await _unitOfWork.WorkingHours.Query()
            .Where(wh => wh.TenantId == tenantId && wh.StaffMemberId == null && wh.DayOfWeek == appointmentDayOfWeek)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantWorkingHours != null && !tenantWorkingHours.IsWorkingDay)
            throw new ValidationException("Nije moguće zakazati termin na neradni dan.");

        if (tenantWorkingHours != null && tenantWorkingHours.IsWorkingDay)
        {
            var apptTime = request.StartTime.TimeOfDay;
            if (apptTime < tenantWorkingHours.StartTime || apptTime >= tenantWorkingHours.EndTime)
                throw new ValidationException($"Termin mora biti u okviru radnog vremena ({tenantWorkingHours.StartTime:hh\\:mm} - {tenantWorkingHours.EndTime:hh\\:mm}).");
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

        // Send confirmation notifications (fire-and-forget, don't block the response)
        _ = Task.Run(async () =>
        {
            try
            {
                // Skip if client has neither email nor phone
                if (string.IsNullOrWhiteSpace(client.Email) && string.IsNullOrWhiteSpace(client.Phone)) return;

                var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, CancellationToken.None);
                if (tenant == null) return;

                var serviceNamesList = string.Join(", ", services.Select(s => s.Name));

                // Send email if client has email
                if (!string.IsNullOrWhiteSpace(client.Email))
                {
                    var emailDto = new AppointmentEmailDto(
                        ClientName: client.FullName,
                        ClientEmail: client.Email,
                        SalonName: tenant.Name,
                        StaffName: staffMember.FullName,
                        StartTime: appointment.StartTime,
                        DurationMinutes: appointment.TotalDurationMinutes,
                        ServiceNames: serviceNamesList,
                        TotalPrice: appointment.TotalPrice,
                        Currency: tenant.Currency ?? "RSD",
                        SalonPhone: tenant.Phone,
                        SalonAddress: tenant.Address
                    );

                    await _emailService.SendAppointmentConfirmationAsync(emailDto);
                }

                // Send SMS confirmation if client has a phone number
                if (!string.IsNullOrWhiteSpace(client.Phone))
                {
                    var smsDto = new AppointmentSmsDto(
                        ClientName: client.FullName,
                        ClientPhone: client.Phone,
                        SalonName: tenant.Name,
                        StaffName: staffMember.FullName,
                        StartTime: appointment.StartTime,
                        DurationMinutes: appointment.TotalDurationMinutes,
                        ServiceNames: serviceNamesList,
                        TotalPrice: appointment.TotalPrice,
                        Currency: tenant.Currency ?? "RSD",
                        SalonPhone: tenant.Phone
                    );

                    await _smsService.SendAppointmentConfirmationAsync(smsDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment confirmation for appointment {AppointmentId}", appointment.Id);
            }
        }, cancellationToken);

        return appointment.Id;
    }
}
