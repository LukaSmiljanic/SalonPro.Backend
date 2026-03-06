using MediatR;

namespace SalonPro.Application.Features.Appointments.Commands.CompletePastAppointments;

/// <summary>
/// Marks all appointments in the current tenant where EndTime has passed
/// and status is not Completed/Cancelled/NoShow as Completed.
/// Returns the number of appointments updated.
/// </summary>
public record CompletePastAppointmentsCommand : IRequest<int>;
