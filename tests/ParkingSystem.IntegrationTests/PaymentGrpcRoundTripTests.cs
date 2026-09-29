using Booking.Application.Common.Models.Exceptions;
using Booking.Infrastructure.GRPC.Client;
using Grpc.Core;
using Payment.Infrastructure.GRPC.Services;
using ParkingSystem.Grpc.Payment;

namespace ParkingSystem.IntegrationTests;

/// <summary>Real gRPC over an in-memory server: booking asks the payment service about debt.</summary>
public class PaymentGrpcRoundTripTests
{
    [Fact]
    public async Task CheckDebt_NotImplementedYet_ServerAnswersUnimplemented()
    {
        await using var payment = await GrpcTestHost.StartAsync<PaymentGrpcService>();
        var client = new PaymentService.PaymentServiceClient(payment.Channel);

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.CheckDebtAsync(new CheckDebtRequest { PlateNumber = "51A-12345", LotId = Guid.NewGuid().ToString() }));

        Assert.Equal(StatusCode.Unimplemented, error.StatusCode);
    }

    [Fact]
    public async Task CheckDebtAsync_NotImplementedYet_ClientReportsNotImplementedInsteadOfNoDebt()
    {
        await using var payment = await GrpcTestHost.StartAsync<PaymentGrpcService>();
        var bookingSide = new PaymentGrpcClient(new PaymentService.PaymentServiceClient(payment.Channel));

        await Assert.ThrowsAsync<RemoteCallNotImplementedException>(() =>
            bookingSide.CheckDebtAsync("51A-12345", Guid.NewGuid(), CancellationToken.None));
    }
}
