using System.Net;
using System.Text.Json;
using Booking.Application;
using Booking.Application.Common.Interfaces.Grpc;
using Booking.Application.Common.Models.Exceptions;
using Booking.Application.DTOs;
using Booking.WebAPI.Controllers;
using Booking.WebAPI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Booking.Tests;

/// <summary>
/// Calls GET /api/booking/lots/{lotId}/availability through a real in-memory pipeline (controller, MediatR,
/// validation, error middleware) and checks the body against the API Design Template.
/// </summary>
public class AvailabilityEndpointTests
{
    private static readonly Guid LotId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Get_KnownLot_Returns200WithTheStandardBody()
    {
        using var host = await StartAsync(new FakeUnitOfGrpc(
            new SlotStatusDto("A-01", "Available", DateTimeOffset.UtcNow),
            new SlotStatusDto("A-02", "Occupied", DateTimeOffset.UtcNow)));

        var response = await host.GetTestClient().GetAsync($"/api/booking/lots/{LotId}/availability");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(200, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("Lot availability retrieved successfully.", body.RootElement.GetProperty("message").GetString());
        var result = body.RootElement.GetProperty("result");
        Assert.Equal(2, result.GetProperty("total").GetInt32());
        Assert.Equal(1, result.GetProperty("available").GetInt32());
        Assert.Equal(1, result.GetProperty("occupied").GetInt32());
    }

    [Fact]
    public async Task Get_EmptyLotId_Returns400WithTheValidationMessage()
    {
        using var host = await StartAsync(new FakeUnitOfGrpc());

        var response = await host.GetTestClient().GetAsync($"/api/booking/lots/{Guid.Empty}/availability");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("result").ValueKind);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(400, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("lotId is missing.", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Get_ParkingServiceDown_Returns503WithTheStandardBody()
    {
        using var host = await StartAsync(new FakeUnitOfGrpc { Failure = new ParkingUnavailableException(Resources.ParkingServiceUnavailable, new Exception()) });

        var response = await host.GetTestClient().GetAsync($"/api/booking/lots/{LotId}/availability");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(503, body.RootElement.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task Get_UnexpectedFailure_Returns500WithoutLeakingTheExceptionMessage()
    {
        using var host = await StartAsync(new FakeUnitOfGrpc { Failure = new InvalidOperationException("connection string password=secret") });

        var response = await host.GetTestClient().GetAsync($"/api/booking/lots/{LotId}/availability");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("secret", text);
        Assert.Contains("\"statusCode\":500", text);
    }

    [Fact]
    public async Task Get_LotIdIsNotAGuid_Returns404WithTheStandardBody()
    {
        using var host = await StartAsync(new FakeUnitOfGrpc());

        var response = await host.GetTestClient().GetAsync("/api/booking/lots/abc/availability");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("result").ValueKind);
        Assert.False(body.RootElement.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(404, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("The requested resource was not found.", body.RootElement.GetProperty("message").GetString());
    }

    private static async Task<IHost> StartAsync(IUnitOfGrpc unitOfGrpc) =>
        await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddControllers()
                        .AddApplicationPart(typeof(AvailabilityController).Assembly)
                        .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ErrorExceptionHandler.InvalidModelState);
                    services.AddApplicationServices(new ConfigurationBuilder().Build());
                    services.AddSingleton(unitOfGrpc);
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
