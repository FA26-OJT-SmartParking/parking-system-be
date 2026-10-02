using System.Security.Cryptography;
using Identity.Application.Common.Models.JwT;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Tests;

public class AccessTokenServiceTests
{
    private readonly TestKeys keys = new();

    private AccessTokenService CreateService() => new(new JwtOptions { Issuer = "test-issuer", PrivateKey = keys.PrivateKey });

    [Fact]
    public async Task GenerateAccessToken_IsRs256AndValidatesWithOnlyThePublicKey()
    {
        using var service = CreateService();
        var account = new UserAccount { Id = Guid.NewGuid(), UserName = "someone" };

        var token = service.GenerateAccessToken(account);

        using var publicKey = RSA.Create();
        publicKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(keys.PublicKey), out _);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidateAudience = false,
            IssuerSigningKey = new RsaSecurityKey(publicKey),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        });

        Assert.True(result.IsValid);
        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal("RS256", jwt.Alg);
        Assert.Equal(account.Id.ToString(), jwt.Subject);
        Assert.InRange(token.ExpiresOn - DateTimeOffset.UtcNow, TimeSpan.FromHours(23.9), TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task GenerateAccessToken_IsRejectedWithAnotherPublicKey()
    {
        using var service = CreateService();
        var token = service.GenerateAccessToken(new UserAccount { Id = Guid.NewGuid(), UserName = "someone" });

        using var otherKey = RSA.Create(2048);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidateAudience = false,
            IssuerSigningKey = new RsaSecurityKey(otherKey),
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GenerateRefreshToken_IsRandomAndExpiresInSevenDays()
    {
        using var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.InRange(first.ExpiresOn - DateTimeOffset.UtcNow, TimeSpan.FromDays(6.9), TimeSpan.FromDays(7));
    }

    [Fact]
    public void Hash_IsSha256HexAndIsNotTheToken()
    {
        using var service = CreateService();

        var hash = service.Hash("some-token");

        Assert.Equal(64, hash.Length);
        Assert.NotEqual("some-token", hash);
        Assert.Equal(hash, service.Hash("some-token"));
    }
}
