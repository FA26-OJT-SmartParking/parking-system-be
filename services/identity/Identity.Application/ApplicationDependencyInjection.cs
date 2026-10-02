using FluentValidation;
using Identity.Application.Common.Behaviors;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Common.Models.JwT;
using Identity.Application.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(ApplicationDependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Every request goes through validation before its handler runs
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));


        var jwtOptions = new JwtOptions();
        configuration.GetSection("Jwt").Bind(jwtOptions);
        if (string.IsNullOrWhiteSpace(jwtOptions.PrivateKey))
        {
            throw new InvalidOperationException("Jwt:PrivateKey is not configured. Generate a key pair with deploy/generate-jwt-keys.sh and set it in deploy/.env.");
        }

        services.AddSingleton(jwtOptions);
        services.AddSingleton<IAccessTokenService, AccessTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
