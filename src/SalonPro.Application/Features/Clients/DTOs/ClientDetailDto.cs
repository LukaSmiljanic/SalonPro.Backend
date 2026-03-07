namespace SalonPro.Application.Features.Clients.DTOs;

public record ClientNoteDto(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    string? CreatedBy
);

public record VisitHistoryDto(
    DateTime Date,
    string ServiceName,
    string StaffName,
    decimal Price
);

public record ClientDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string Phone,
    string? Notes,
    bool IsVip,
    string? Tags,
    int TotalVisits,
    decimal TotalSpent,
    DateTime? LastVisitDate,
    List<VisitHistoryDto> VisitHistory,
    List<ClientNoteDto> ClientNotes,
    ClientLoyaltyDto? Loyalty
);
