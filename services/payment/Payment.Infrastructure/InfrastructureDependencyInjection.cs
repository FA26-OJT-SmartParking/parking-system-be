using Payment.Application.Common.Interfaces.MessageBroker;
using Payment.Infrastructure.MessageBroker;
using Payment.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistenceServices(configuration);
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddMessageBroker(configuration);

        return services;
    }

    /// <summary>
    /// MassTransit over RabbitMQ. Messages published inside a request are stored in the outbox of ApplicationDbContext
    /// and sent after SaveChanges; consumed messages go through the inbox, so a message delivered twice is handled once.
    /// </summary>
    private static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(MessageBrokerSettings)).Get<MessageBrokerSettings>();
        if (settings is null || string.IsNullOrWhiteSpace(settings.HostName)
            || string.IsNullOrWhiteSpace(settings.UserName) || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException("MessageBrokerSettings (HostName, UserName, Password) is not configured. Set it in deploy/.env or with dotnet user-secrets.");
        }

        services.AddMassTransit(x =>
        {
            // Finds every consumer of this service automatically
            x.AddConsumers(typeof(InfrastructureDependencyInjection).Assembly);
            x.AddEntityFrameworkOutbox<ApplicationDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
            x.AddConfigureEndpointsCallback((context, _, endpoint) =>
                endpoint.UseEntityFrameworkOutbox<ApplicationDbContext>(context));
            x.UsingRabbitMq((context, bus) =>
            {
                bus.Host(settings.HostName, (ushort)settings.Port, settings.VirtualHost, host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });
                bus.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
