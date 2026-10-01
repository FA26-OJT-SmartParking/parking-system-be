using System.Net;
using System.Text.Json;
using Identity.Application;
using Identity.Application.Common.Interfaces.Persistence;
using Identity.Persistence;
using Identity.Persistence.Repositories;
using Identity.WebAPI.Controllers;
using Identity.WebAPI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Identity.Tests;

/// <summary>
/// Calls GET /api/identity/users/sample through a real in-memory pipeline (controller, MediatR, validation,
/// unit of work, repository, error middleware). No database is needed: the repository builds the user in memory.
/// </summary>
public class SampleUserEndpointTests
{
    [Fact]
    public async Task GetSample_WithName_Returns200WithTheUser()
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().GetAsync("/api/identity/users/sample?name=Loc");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(200, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("Sample user retrieved successfully.", body.RootElement.GetProperty("message").GetString());
        var result = body.RootElement.GetProperty("result");
        Assert.Equal("11111111-1111-1111-1111-111111111111", result.GetProperty("id").GetString());
        Assert.Equal("Loc", result.GetProperty("name").GetString());
        Assert.Equal("sample.user@example.com", result.GetProperty("email").GetString());
        Assert.Equal("0900000000", result.GetProperty("phoneNumber").GetString());
    }

    [Theory]
    [InlineData("/api/identity/users/sample")]
    [InlineData("/api/identity/users/sample?name=")]
    [InlineData("/api/identity/users/sample?name=%20")]
    public async Task GetSample_WithoutName_Returns400WithTheValidationMessage(string url)
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().GetAsync(url);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("result").ValueKind);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(400, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("name is missing.", body.RootElement.GetProperty("message").GetString());
    }

    private static async Task<IHost> StartAsync() =>
        await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers()
                        .AddApplicationPart(typeof(UserController).Assembly)
                        .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ErrorExceptionHandler.InvalidModelState);
                    services.AddApplicationServices(new ConfigurationBuilder().Build());
                    // Creating the context opens no connection, and the sample user never uses it
                    services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql("Host=localhost"));
                    services.AddScoped<IUnitOfWork, UnitOfWork>();
                    services.AddTransient<ExceptionHandlingMiddleware>();
                    services.AddRouting();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.UseStatusCodePages(ErrorExceptionHandler.WriteStatusCodeBody);
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .StartAsync();
}
