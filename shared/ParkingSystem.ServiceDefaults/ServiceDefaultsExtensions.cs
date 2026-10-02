using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ParkingSystem.ServiceDefaults;

/// <summary>Setup shared by the gateway and every .NET service.</summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Serilog, OpenTelemetry traces and metrics, health checks and JWT validation.
    /// Logs, traces and metrics go to OTEL_EXPORTER_OTLP_ENDPOINT (the Aspire Dashboard) when it is set.
    /// </summary>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        builder.AddServiceLogging(serviceName, otlpEndpoint);
        builder.AddServiceTelemetry(serviceName, otlpEndpoint);
        builder.Services.AddHealthChecks();
        builder.AddJwtValidation();

        return builder;
    }

    /// <summary>
    /// Request logging, authentication, authorization and the /health endpoint (open to everyone, Docker checks it).
    /// <paramref name="afterRequestLogging"/> adds middleware that must run after logging and before authentication.
    /// </summary>
    public static WebApplication UseServiceDefaults(this WebApplication app, Action<IApplicationBuilder>? afterRequestLogging = null)
    {
        app.UseSerilogRequestLogging();
        afterRequestLogging?.Invoke(app);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health").AllowAnonymous();
        return app;
    }
}
