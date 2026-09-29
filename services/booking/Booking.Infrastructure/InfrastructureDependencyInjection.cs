using Booking.Application.Interfaces;
using Booking.Infrastructure.Grpc;
using ParkingSystem.Grpc.Parking;
using ParkingSystem.Grpc.Payment;
using Booking.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistenceServices(configuration);

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
}
