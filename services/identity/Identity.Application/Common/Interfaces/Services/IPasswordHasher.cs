namespace Identity.Application.Common.Interfaces.Services;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// True when the password matches the hash. With no hash (unknown user) it does the same work and returns
    /// false, so the time taken does not reveal whether the user exists.
    /// </summary>
    bool Verify(string password, string? hash);
}
