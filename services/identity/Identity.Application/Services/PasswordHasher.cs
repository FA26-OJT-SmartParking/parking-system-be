using Identity.Application.Common.Interfaces.Services;

namespace Identity.Application.Services;

/// <summary>bcrypt with cost 12 (NFR-SEC-001).</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    private static readonly string UnknownUserHash = BCrypt.Net.BCrypt.HashPassword("unknown-user", WorkFactor);

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string? hash)
    {
        if (hash is null)
        {
            BCrypt.Net.BCrypt.Verify(password, UnknownUserHash);
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
