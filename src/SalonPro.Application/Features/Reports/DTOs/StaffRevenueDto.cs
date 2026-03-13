namespace SalonPro.Application.Features.Reports.DTOs;

public record StaffRevenueDto(
    string StaffId,
    string StaffName,
    decimal TotalRevenue,
    int AppointmentCount,
    decimal AveragePerAppointment
);
