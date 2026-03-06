using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Services.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Services.Queries.GetServicesByCategory;

public class GetServicesByCategoryQueryHandler : IRequestHandler<GetServicesByCategoryQuery, List<ServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServicesByCategoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceDto>> Handle(GetServicesByCategoryQuery request, CancellationToken cancellationToken)
    {
        var services = await _unitOfWork.Services.Query()
            .Include(s => s.Category)
            .Where(s => s.CategoryId == request.CategoryId && s.IsActive)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return services.Select(s => new ServiceDto(
            s.Id,
            s.CategoryId,
            s.Category?.Name ?? "",
            s.Name,
            s.Description,
            s.DurationMinutes,
            s.Price,
            s.IsActive,
            Domain.Enums.ServiceCategoryType.Hair,
            ""
        )).ToList();
    }
}
