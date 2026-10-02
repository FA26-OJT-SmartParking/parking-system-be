using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ParkingSystem.ServiceDefaults;

namespace Booking.Tests;

/// <summary>Controllers ask for a signed-in user unless an action says it is public, and unknown routes still answer 404.</summary>
public class ControllersRequireSignInTests
{
    private const string Issuer = "test-issuer";

    [Fact]
    public async Task Get_ActionMarkedAllowAnonymous_Returns200WithoutAToken()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await app.GetTestClient().GetAsync("/probe/open");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ActionWithoutAnyMark_Returns401WithTheStandardBody()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await app.GetTestClient().GetAsync("/probe/closed");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(401, body.RootElement.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task Get_ActionWithoutAnyMark_Returns200WithATokenFromTheIdentityService()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await Send(app, "/probe/closed", Token(identityKey));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ActionWithoutAnyMark_Returns401WithATokenSignedByAnotherKey()
    {
        using var identityKey = RSA.Create(2048);
        using var otherKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await Send(app, "/probe/closed", Token(otherKey));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownRoute_StillReturns404ForAnonymousCallers()
    {
        using var identityKey = RSA.Create(2048);
        await using var app = await StartAsync(identityKey);

        var response = await app.GetTestClient().GetAsync("/probe/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        builder.Services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);

        var app = builder.Build();
        app.UseServiceDefaults();
        // The same line every service has in Program.cs
        app.MapControllers().RequireAuthorization();
        await app.StartAsync();
        return app;
    }
}

/// <summary>A controller with one public and one unmarked action. ASP.NET only finds controllers that are not nested.</summary>
[ApiController]
[Route("probe")]
public class ProbeController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("open")]
    public IActionResult Open() => Ok("open");

    [HttpGet("closed")]
    public IActionResult Closed() => Ok("closed");
}
