namespace Booking.Application.Common.Interfaces.Grpc;

/// <summary>Every gRPC client this service uses, in one place.</summary>
public interface IUnitOfGrpc
{
    IParkingGrpcClient ParkingGrpcClient { get; }
}
