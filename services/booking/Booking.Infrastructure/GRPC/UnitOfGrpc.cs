using Booking.Application.Common.Interfaces.Grpc;

namespace Booking.Infrastructure.GRPC;

public class UnitOfGrpc(IParkingGrpcClient parkingGrpcClient, IPaymentGrpcClient paymentGrpcClient) : IUnitOfGrpc
{
    public IParkingGrpcClient ParkingGrpcClient { get; } = parkingGrpcClient;

    public IPaymentGrpcClient PaymentGrpcClient { get; } = paymentGrpcClient;
}
