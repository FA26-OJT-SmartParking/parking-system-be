using Booking.Application.Common.Interfaces.Grpc;

namespace Booking.Infrastructure.GRPC;

public class UnitOfGrpc(IParkingGrpcClient parkingGrpcClient) : IUnitOfGrpc
{
    public IParkingGrpcClient ParkingGrpcClient { get; } = parkingGrpcClient;
}
