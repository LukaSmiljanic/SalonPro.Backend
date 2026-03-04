using AutoMapper;
using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;
    private readonly IDateTimeService _dateTimeService;

    public RegisterCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IMapper mapper,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
        _dateTimeService = dateTimeService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLower(), cancellationToken);

        if (existingUser != null)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Email is already in use.") });

        // Check if tenant slug already exists
        var existingTenant = await _unitOfWork.Tenants.FirstOrDefaultAsync(
            t => t.Slug == request.TenantSlug.ToLower(), cancellationToken);

        if (existingTenant != null)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("TenantSlug", "Tenant slug is already taken.") });

        // Create tenant
        var tenant = new Tenant
        {
            Name = request.TenantName,
            Slug = request.TenantSlug.ToLower(),
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow
        };

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);

        // Create admin user
        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.Email.ToLower(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = _dateTimeService.UtcNow.AddDays(7);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<AuthResponseDto>(user);
        response.AccessToken = accessToken;
        response.RefreshToken = refreshToken;
        response.ExpiresAt = _dateTimeService.UtcNow.AddMinutes(60);

        return response;
    }
}
