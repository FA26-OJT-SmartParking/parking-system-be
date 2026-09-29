using Identity.Domain.Base;

namespace Identity.Domain.Entities;

/// <summary>One login. Only hashes of the tokens are stored, never the tokens themselves.</summary>
public class UserAccountSession : BaseEntity
{
    public string? AccessTokenHash { get; set; }

    public string? RefreshTokenHash { get; set; }

    /// <summary>When the refresh token stops being valid.</summary>
    public DateTimeOffset? ExpiredOn { get; set; }

    public Guid? UserAccountId { get; set; }

    public UserAccount? UserAccount { get; set; }
}
