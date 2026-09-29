using Booking.Application.Interfaces;
using Booking.Infrastructure.Grpc;
using ParkingSystem.Grpc.Parking;
using ParkingSystem.Grpc.Payment;
using Booking.Application.Common.Interfaces.MessageBroker;
using Booking.Infrastructure.MessageBroker;
using Booking.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistenceServices(configuration);
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddMessageBroker(configuration);

        // Address of the parking service gRPC endpoint (plain HTTP/2 inside the Docker network)
        services.AddGrpcClient<ParkingService.ParkingServiceClient>(options =>
            options.Address = new Uri(configuration["Grpc:Parking"] ?? "http://localhost:5112"));
        services.AddScoped<IParkingClient, ParkingGrpcClient>();

        // Address of the payment service gRPC endpoint (plain HTTP/2 inside the Docker network)
        services.AddGrpcClient<PaymentService.PaymentServiceClient>(options =>
            options.Address = new Uri(configuration["Grpc:Payment"] ?? "http://localhost:5114"));
        services.AddScoped<IPaymentClient, PaymentGrpcClient>();

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
