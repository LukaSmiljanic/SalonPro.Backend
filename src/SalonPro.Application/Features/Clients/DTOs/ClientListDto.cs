namespace SalonPro.Application.Features.Clients.DTOs;

public record ClientListDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    DateTime? LastVisitDate,
    string? FavoriteService,
    bool IsVip,
    string? Tags,
    int TotalVisits
);
