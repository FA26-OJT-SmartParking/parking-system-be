using Booking.Application.Exceptions;
using Booking.Application.Interfaces;
using Grpc.Core;
using ParkingSystem.Grpc.Payment;

namespace Booking.Infrastructure.Grpc;

/// <summary>Calls the payment service over gRPC (contract: grpc_proto/payment.proto).</summary>
public class PaymentGrpcClient(PaymentService.PaymentServiceClient client) : IPaymentClient
{
    public async Task<(bool HasDebt, long Amount)> CheckDebtAsync(string plateNumber, Guid lotId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.CheckDebtAsync(new CheckDebtRequest
            {
                PlateNumber = plateNumber,
                LotId = lotId.ToString()
            }, cancellationToken: cancellationToken);

            return (response.HasDebt, response.Amount);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            throw new RemoteCallNotImplementedException("The payment service has not implemented CheckDebt yet.", ex);
        }
    }
}
