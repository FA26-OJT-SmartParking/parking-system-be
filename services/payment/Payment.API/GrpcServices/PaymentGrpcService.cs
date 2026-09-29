using Grpc.Core;
using ParkingSystem.Grpc.Payment;

namespace Payment.API.GrpcServices;

/// <summary>Answers payment queries from other services (contract: grpc_proto/payment.proto).</summary>
public class PaymentGrpcService : PaymentService.PaymentServiceBase
{
    // Not implemented on purpose: answering "no debt" without checking would let indebted vehicles in.
    // Replace with a query of the debt repository by plate and lot when it exists.
    public override Task<CheckDebtReply> CheckDebt(CheckDebtRequest request, ServerCallContext context) =>
        throw new RpcException(new Status(StatusCode.Unimplemented, "CheckDebt is not implemented yet"));
}
