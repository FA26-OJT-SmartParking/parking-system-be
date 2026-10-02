using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ParkingSystem.ServiceDefaults;

namespace Booking.Tests;

/// <summary>An endpoint asks for a signed-in user unless it says it is public.</summary>
public class SecureByDefaultTests
{
    private const string Issuer = "test-issuer";

    [Fact]
    public async Task Get_EndpointMarkedAllowAnonymous_Returns200WithoutAToken()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await app.GetTestClient().GetAsync("/open");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_EndpointWithoutAnyMark_Returns401WithTheStandardBody()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await app.GetTestClient().GetAsync("/closed");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(401, body.RootElement.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task Get_EndpointWithoutAnyMark_Returns200WithATokenFromTheIdentityService()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await Send(app, "/closed", Token(identityKey));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_EndpointWithoutAnyMark_Returns401WithATokenSignedByAnotherKey()
    {
        using var identityKey = RSA.Create(2048);
        using var otherKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await Send(app, "/closed", Token(otherKey));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Health_IsOpenForDocker()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await app.GetTestClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Send(WebApplication app, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new("Bearer", token);
        return app.GetTestClient().SendAsync(request);
    }

    private static string Token(RSA key) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Expires = DateTime.UtcNow.AddMinutes(5),
        SigningCredentials = new SigningCredentials(new RsaSecurityKey(key), SecurityAlgorithms.RsaSha256),
    });

    private static async Task<WebApplication> StartAsync(RSA identityKey)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:PublicKey"] = Convert.ToBase64String(identityKey.ExportSubjectPublicKeyInfo()),
            ["Jwt:Issuer"] = Issuer,
        });
        builder.AddServiceDefaults("test");

        var app = builder.Build();
        app.UseServiceDefaults();
        app.MapGet("/open", () => "open").AllowAnonymous();
        app.MapGet("/closed", () => "closed");
        await app.StartAsync();
        return app;
    }
}
