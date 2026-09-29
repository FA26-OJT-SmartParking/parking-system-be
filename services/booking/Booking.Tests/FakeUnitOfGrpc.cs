using Booking.Application.Common.Interfaces.Grpc;
using Booking.Application.DTOs;

namespace Booking.Tests;

/// <summary>gRPC clients that answer from memory, so the tests need no other service.</summary>
internal sealed class FakeUnitOfGrpc(params SlotStatusDto[] slots) : IUnitOfGrpc
{
    public Exception? Failure { get; init; }

    public IParkingGrpcClient ParkingGrpcClient => new FakeParkingGrpcClient(slots, Failure);

    public IPaymentGrpcClient PaymentGrpcClient => throw new NotSupportedException("Not used by these tests");

    private sealed class FakeParkingGrpcClient(SlotStatusDto[] slots, Exception? failure) : IParkingGrpcClient
    {
        public Task<IReadOnlyList<SlotStatusDto>> GetLotSlotsAsync(Guid lotId, CancellationToken cancellationToken) =>
            failure is null ? Task.FromResult<IReadOnlyList<SlotStatusDto>>(slots) : Task.FromException<IReadOnlyList<SlotStatusDto>>(failure);

        public Task<(bool Success, string SlotCode, string ErrorCode)> AssignSlotAsync(Guid reservationId, Guid lotId, string vehicleType, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not used by these tests");
    }
}
