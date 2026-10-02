using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ParkingSystem.ServiceDefaults;

/// <summary>Checks the access token of every request against the public key of the identity service.</summary>
internal static class JwtAuthenticationExtensions
{
    public static WebApplicationBuilder AddJwtValidation(this WebApplicationBuilder builder)
    {
        // Tokens are signed by the identity service with its private key (RS256, NFR-SEC-003);
        // the gateway and every service only hold the public key, so none of them can issue a token.
        var publicKey = builder.Configuration["Jwt:PublicKey"]
            ?? throw new InvalidOperationException("Jwt:PublicKey is not configured (set it in deploy/.env or with dotnet user-secrets).");
        var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
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

    private static Task WriteErrorAsync(HttpResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        return response.WriteAsJsonAsync(new { result = (object?)null, isSuccess = false, statusCode, message });
    }
}
