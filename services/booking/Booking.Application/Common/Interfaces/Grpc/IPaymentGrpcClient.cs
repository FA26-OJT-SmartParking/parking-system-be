namespace Booking.Application.Common.Interfaces.Grpc;

/// <summary>Asks the payment service about vehicle debt (contract: grpc_proto/payment.proto, BR-05).</summary>
public interface IPaymentGrpcClient
{
    /// <exception cref="Models.Exceptions.RemoteCallNotImplementedException">The payment service has not implemented CheckDebt yet.</exception>
    Task<(bool HasDebt, long Amount)> CheckDebtAsync(string plateNumber, Guid lotId, CancellationToken cancellationToken);
}
