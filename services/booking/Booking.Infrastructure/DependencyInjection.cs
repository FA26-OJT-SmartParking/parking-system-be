using Booking.Application.Interfaces;
using Booking.Infrastructure.Grpc;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkingSystem.Grpc.Parking;
using ParkingSystem.Grpc.Payment;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BookingDb>(options => options.UseNpgsql(configuration.GetConnectionString("Db")));

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
