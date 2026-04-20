using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Appointments.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Common;

/// <summary>
/// Projects which visit number a calendar appointment represents (1-based) and whether it matches a loyalty tier threshold (jubilee visit).
/// </summary>
public static class AppointmentLoyaltyProjection
{
    public static readonly int[] DefaultMilestoneThresholds = [10, 25, 50, 100];

    public static async Task<IReadOnlyList<int>> LoadMilestoneThresholdsAsync(
        Guid tenantId,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var fromDb = await unitOfWork.LoyaltyConfigs.Query()
            .Where(lc => lc.TenantId == tenantId)
            .OrderBy(lc => lc.MinVisits)
            .Select(lc => lc.MinVisits)
            .Distinct()
            .ToListAsync(cancellationToken);

        return fromDb.Count > 0 ? fromDb : DefaultMilestoneThresholds;
    }

    /// <summary>
    /// Builds per-client ordered lists of completed visits (by start time, then id).
    /// </summary>
    public static Dictionary<Guid, List<(Guid Id, DateTime StartTime)>> IndexCompletedVisits(
        IReadOnlyCollection<(Guid Id, Guid ClientId, DateTime StartTime)> completedRows)
    {
        return completedRows
            .GroupBy(x => x.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.StartTime).ThenBy(x => x.Id).Select(x => (x.Id, x.StartTime)).ToList());
    }

    /// <summary>
    /// Visit number = 1 + count of completed appointments for this client that strictly precede this slot in (StartTime, Id) order.
    /// </summary>
    public static (int VisitNumber, bool IsLoyaltyMilestoneVisit) ComputeVisitInfo(
        Guid appointmentId,
        Guid clientId,
        DateTime startTime,
        IReadOnlyDictionary<Guid, List<(Guid Id, DateTime StartTime)>> completedIndex,
        IReadOnlyList<int> milestoneThresholds)
    {
        var prior = 0;
        if (completedIndex.TryGetValue(clientId, out var list))
        {
            prior = list.Count(x =>
                x.StartTime < startTime ||
                (x.StartTime == startTime && x.Id.CompareTo(appointmentId) < 0));
        }

        var visitNumber = prior + 1;
        var isMilestone = milestoneThresholds.Contains(visitNumber);
        return (visitNumber, isMilestone);
    }

    public static AppointmentDto ToAppointmentDto(
        Appointment a,
        int visitNumber,
        bool isLoyaltyMilestoneVisit)
    {
        return new AppointmentDto(
            a.Id,
            a.Client.FullName,
            a.StaffMember.FullName,
            a.StartTime,
            a.EndTime,
            a.Status,
            a.TotalPrice,
            a.Notes,
            a.AppointmentServices.Select(aps => new AppointmentServiceDto(
                aps.ServiceId,
                aps.Service.Name,
                aps.Price,
                aps.DurationMinutes)).ToList(),
            visitNumber,
            isLoyaltyMilestoneVisit);
    }
}
