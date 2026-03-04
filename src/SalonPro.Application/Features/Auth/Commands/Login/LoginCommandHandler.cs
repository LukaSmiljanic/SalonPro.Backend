using AutoMapper;
using MediatR;
using SalonPro.Application.Common.Exceptions;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Application.Features.Auth.DTOs;
using SalonPro.Domain.Interfaces;

namespace SalonPro.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;
    private readonly IDateTimeService _dateTimeService;

    public LoginCommandHandler(
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

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLower() && u.IsActive, cancellationToken);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

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
