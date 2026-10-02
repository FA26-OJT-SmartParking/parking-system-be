using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ParkingSystem.ServiceDefaults;

/// <summary>OpenTelemetry traces and metrics, exported to the OTLP endpoint (the Aspire Dashboard) when it is set.</summary>
internal static class TelemetryExtensions
{
    public static WebApplicationBuilder AddServiceTelemetry(this WebApplicationBuilder builder, string serviceName, string? otlpEndpoint)
    {
        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
