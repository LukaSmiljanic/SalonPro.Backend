using MediatR;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Features.Services.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Services.Queries.GetServices;

public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, List<ServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetServicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ServiceDto>> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Services.Query()
            .Include(s => s.Category)
            .AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var services = await query
            .OrderBy(s => s.Category != null ? s.Category.Name : "")
            .ThenBy(s => s.Name)
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
            Domain.Enums.ServiceCategoryType.Massage,
            ""
        )).ToList();
    }
}
