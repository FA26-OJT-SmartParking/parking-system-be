using Identity.Application.Common.Models.JwT;
using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces.Services;

public interface IAccessTokenService
{
    /// <summary>A JWT signed with RS256 for the account.</summary>
    AccessToken GenerateAccessToken(UserAccount account);

    /// <summary>A random refresh token and the moment it expires.</summary>
    RefreshToken GenerateRefreshToken();

    /// <summary>SHA-256 of a token, as stored in the database. The tokens themselves are never stored.</summary>
    string Hash(string token);
}
