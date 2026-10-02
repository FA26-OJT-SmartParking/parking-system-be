using Identity.Application.Services;

namespace Identity.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher hasher = new();

    [Fact]
    public void Hash_UsesBcryptWithCost12()
    {
        Assert.StartsWith("$2a$12$", hasher.Hash("Correct-Horse-9!"));
    }

    [Fact]
    public void Verify_RightAndWrongPassword()
    {
        var hash = hasher.Hash("Correct-Horse-9!");

        Assert.True(hasher.Verify("Correct-Horse-9!", hash));
        Assert.False(hasher.Verify("correct-horse-9!", hash));
    }

    [Fact]
    public void Verify_UnknownUser_ReturnsFalse()
    {
        Assert.False(hasher.Verify("anything", null));
    }

    [Fact]
    public void Verify_HashThatIsNotBcrypt_ReturnsFalse()
    {
        Assert.False(hasher.Verify("anything", "not-a-bcrypt-hash"));
    }
}
