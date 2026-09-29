using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Common.Models.JwT;
using Identity.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Application.Services;

public sealed class AccessTokenService : IAccessTokenService, IDisposable
{
    private readonly JwtOptions options;
    private readonly RSA rsa = RSA.Create();
    private readonly SigningCredentials credentials;
    private readonly JsonWebTokenHandler handler = new();

    public AccessTokenService(JwtOptions options)
    {
        this.options = options;
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(options.PrivateKey), out _);
        credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
    }

    public AccessToken GenerateAccessToken(UserAccount account)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresOn = now + options.AccessTokenLifetime;
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresOn.UtcDateTime,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, account.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        });
        return new AccessToken(token, expiresOn);
    }

    public RefreshToken GenerateRefreshToken() =>
        new(Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)), DateTimeOffset.UtcNow + options.RefreshTokenLifetime);

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public void Dispose() => rsa.Dispose();
}
