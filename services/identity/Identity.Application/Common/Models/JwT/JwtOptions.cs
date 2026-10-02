namespace Identity.Application.Common.Models.JwT;

/// <summary>Token settings, read from the Jwt configuration section (NFR-SEC-003: RS256, 24 hours, 7 days).</summary>
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Base64 of the PKCS#8 private key. Only the identity service has it; the others get the public key.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(24);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
