using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

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

        builder.Services.AddHealthChecks();

        // Tokens are issued by the identity service and checked by the gateway and by each service (BR-11 claims come later).
        var signingKey = builder.Configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured (set it in .env or with dotnet user-secrets).");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                };

                // 401 and 403 use the same body as every other error (see the API Design Template)
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteErrorAsync(context.Response, StatusCodes.Status401Unauthorized, "You are not signed in. Sign in and try again.");
                    },
                    OnForbidden = context =>
                        WriteErrorAsync(context.Response, StatusCodes.Status403Forbidden, "You do not have permission to do this."),
                };
            });
        builder.Services.AddAuthorization();

        return builder;
    }

    /// <summary>
    /// Request logging, authentication, authorization and the /health endpoint.
    /// <paramref name="afterRequestLogging"/> adds middleware that must run after logging and before authentication.
    /// </summary>
    public static WebApplication UseServiceDefaults(this WebApplication app, Action<IApplicationBuilder>? afterRequestLogging = null)
    {
        app.UseSerilogRequestLogging();
        afterRequestLogging?.Invoke(app);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        return app;
    }

    /// <summary>
    /// Applies the EF Core migrations of <typeparamref name="TDbContext"/> on startup in Development.
    /// Elsewhere, run them as a deployment step (dotnet ef database update) before the new version starts.
    /// </summary>
    public static WebApplication MigrateDatabase<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TDbContext>().Database.Migrate();
        }
        return app;
    }

    private static Task WriteErrorAsync(HttpResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        return response.WriteAsJsonAsync(new { result = (object?)null, isSuccess = false, statusCode, message });
    }
}
