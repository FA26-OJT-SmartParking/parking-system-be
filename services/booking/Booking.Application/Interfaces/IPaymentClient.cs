namespace Booking.Application.Interfaces;

/// <summary>Calls payment service to verify vehicle debt status (contract: grpc_proto/payment.proto, BR-05).</summary>
public interface IPaymentClient
{
    Task<(bool HasDebt, long Amount)> CheckDebtAsync(string plateNumber, Guid lotId, CancellationToken cancellationToken);
}
