using System.Security.Cryptography;

namespace Identity.Tests;

/// <summary>A throw-away RS256 key pair, the same format as JWT_PRIVATE_KEY and JWT_PUBLIC_KEY.</summary>
internal sealed class TestKeys
{
    public TestKeys()
    {
        using var rsa = RSA.Create(2048);
        PrivateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        PublicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
    }

    public string PrivateKey { get; }

    public string PublicKey { get; }
}
