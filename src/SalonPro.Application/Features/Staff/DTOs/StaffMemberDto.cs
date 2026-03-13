namespace SalonPro.Application.Features.Staff.DTOs;

public record StaffMemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Specialization,
    string? Email,
    string? Phone,
    bool IsActive,
    int ColorIndex,
    int AppointmentCountToday,
    int TotalAppointments
);
