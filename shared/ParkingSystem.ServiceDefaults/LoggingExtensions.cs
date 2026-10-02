using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace ParkingSystem.ServiceDefaults;

/// <summary>Serilog to the console and, when an OTLP endpoint is set, to the Aspire Dashboard.</summary>
internal static class LoggingExtensions
{
    public static WebApplicationBuilder AddServiceLogging(this WebApplicationBuilder builder, string serviceName, string? otlpEndpoint)
    {
        builder.Services.AddSerilog(logger =>
        {
            logger.ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console();
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                logger.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = serviceName };
                });
            }
        });

        return builder;
    }
}
