using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Identity.Application;
using Identity.Application.Common.Interfaces.Persistence;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Enum;
using Identity.WebAPI.Controllers;
using Identity.WebAPI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Tests;

/// <summary>
/// Calls POST /api/auth/login through a real in-memory pipeline (controller, MediatR, validation, error middleware,
/// bcrypt and RS256) and checks every case of the API Design Template. Only the database is replaced by a fake.
/// </summary>
public class LoginEndpointTests : IAsyncLifetime
{
    private const string UserName = "userName";
    private const string Password = "Correct-Horse-9!";

    private readonly TestKeys keys = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private IHost host = null!;
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        unitOfWork.Accounts.Items.Add(new UserAccount
        {
            Id = Guid.NewGuid(),
            UserName = UserName,
            PasswordHash = new PasswordHasher().Hash(Password),
            Status = AccountStatus.Active,
        });
        unitOfWork.Accounts.Items.Add(new UserAccount
        {
            Id = Guid.NewGuid(),
            UserName = "locked",
            PasswordHash = new PasswordHasher().Hash(Password),
            Status = AccountStatus.Lock,
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Issuer"] = "test-issuer", ["Jwt:PrivateKey"] = keys.PrivateKey })
            .Build();
        host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers()
                        .AddApplicationPart(typeof(AuthController).Assembly)
                        .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ErrorExceptionHandler.InvalidModelState);
                    services.AddApplicationServices(configuration);
                    services.AddSingleton<IUnitOfWork>(unitOfWork);
                    services.AddTransient<ExceptionHandlingMiddleware>();
                    services.AddRouting();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();
        client = host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await host.StopAsync();
        host.Dispose();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokensAndTheTemplateBody()
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = UserName, password = Password });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(200, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("Sign in successfully", body.RootElement.GetProperty("message").GetString());
        var result = body.RootElement.GetProperty("result");
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Login_ValidCredentials_AccessTokenIsRs256AndValidWithThePublicKey()
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = UserName, password = Password });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = body.RootElement.GetProperty("result").GetProperty("accessToken").GetString()!;

        using var publicKey = RSA.Create();
        publicKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(keys.PublicKey), out _);
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidateAudience = false,
            IssuerSigningKey = new RsaSecurityKey(publicKey),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        });

        Assert.True(validation.IsValid);
        Assert.Equal(unitOfWork.Accounts.Items[0].Id.ToString(), ((JsonWebToken)validation.SecurityToken).Subject);
    }

    [Fact]
    public async Task Login_ValidCredentials_SavesTheSessionWithHashesOnly()
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = UserName, password = Password });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var refreshToken = body.RootElement.GetProperty("result").GetProperty("refreshToken").GetString()!;

        var session = Assert.Single(unitOfWork.Sessions.Items);
        Assert.Equal(unitOfWork.Accounts.Items[0].Id, session.UserAccountId);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))), session.RefreshTokenHash);
        Assert.NotEqual(refreshToken, session.RefreshTokenHash);
        Assert.InRange(session.ExpiredOn!.Value - DateTimeOffset.UtcNow, TimeSpan.FromDays(6.9), TimeSpan.FromDays(7));
        Assert.Equal(1, unitOfWork.Saves);
        Assert.NotNull(unitOfWork.Accounts.Items[0].LastLogin);
    }

    [Fact]
    public async Task Login_MissingUserName_Returns400()
    {
        await AssertFailure(new { password = Password }, "userName is missing.");
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        await AssertFailure(new { userName = UserName }, "password is missing.");
    }

    [Fact]
    public async Task Login_EmptyBody_ReportsTheMissingUserName()
    {
        var response = await client.PostAsync("/api/auth/login", new StringContent(string.Empty));

        await AssertBody(response, "userName is missing.");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns400WithTheIncorrectMessage()
    {
        await AssertFailure(new { userName = UserName, password = "wrong-password" }, "Incorrect username or password. Try again.");
    }

    [Fact]
    public async Task Login_UnknownUser_GivesTheSameAnswerAsAWrongPassword()
    {
        await AssertFailure(new { userName = "nobody", password = Password }, "Incorrect username or password. Try again.");
    }

    [Fact]
    public async Task Login_LockedAccount_GivesTheSameAnswerAsAWrongPassword()
    {
        await AssertFailure(new { userName = "locked", password = Password }, "Incorrect username or password. Try again.");
    }

    [Fact]
    public async Task Login_Failure_SavesNothing()
    {
        await client.PostAsJsonAsync("/api/auth/login", new { userName = UserName, password = "wrong-password" });

        Assert.Empty(unitOfWork.Sessions.Items);
        Assert.Equal(0, unitOfWork.Saves);
    }

    private async Task AssertFailure(object request, string message) =>
        await AssertBody(await client.PostAsJsonAsync("/api/auth/login", request), message);

    private static async Task AssertBody(HttpResponseMessage response, string message)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("result").ValueKind);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(400, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal(message, body.RootElement.GetProperty("message").GetString());
    }
}
