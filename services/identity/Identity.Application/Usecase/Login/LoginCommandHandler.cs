using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Common.Enums;
using Identity.Application.Common.Interfaces.Persistence;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Enum;
using MediatR;

namespace Identity.Application.Usecase.Login;

public class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IAccessTokenService accessTokenService,
    IPasswordHasher passwordHasher) : IRequestHandler<LoginCommand, UserSessionDto>
{
    public async Task<UserSessionDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountRepository.GetByUserNameAsync(request.UserName!, cancellationToken);

        // Same work and same answer whether the user exists, is locked or typed a wrong password
        var passwordMatches = passwordHasher.Verify(request.Password!, account?.PasswordHash);
        if (account is null || !passwordMatches || account.Status != AccountStatus.Active)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Password), Resources.IncorrectUsernameOrPassword)
                {
                    ErrorCode = nameof(ErrorCode.IncorrectUsernameOrPassword),
                },
            ]);
        }

        var accessToken = accessTokenService.GenerateAccessToken(account);
        var refreshToken = accessTokenService.GenerateRefreshToken();

        // Save the login with hashes of the tokens and the moment the refresh token expires
        await unitOfWork.UserAccountSessionRepository.AddAsync(new UserAccountSession
        {
            UserAccountId = account.Id,
            AccessTokenHash = accessTokenService.Hash(accessToken.Value),
            RefreshTokenHash = accessTokenService.Hash(refreshToken.Value),
            ExpiredOn = refreshToken.ExpiresOn,
        }, cancellationToken);
        account.LastLogin = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserSessionDto { AccessToken = accessToken.Value, RefreshToken = refreshToken.Value };
    }
}
