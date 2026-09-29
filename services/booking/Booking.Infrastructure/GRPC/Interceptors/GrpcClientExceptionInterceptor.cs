using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure.GRPC.Interceptors;

/// <summary>Logs every outgoing gRPC call and the errors it returns; the exception is passed on unchanged.</summary>
public class GrpcClientExceptionInterceptor(ILogger<GrpcClientExceptionInterceptor> logger) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        logger.LogInformation("Calling gRPC {Method}", context.Method.FullName);

        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponseAsync(call.ResponseAsync, context.Method.FullName),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponseAsync<TResponse>(Task<TResponse> responseTask, string methodName)
    {
        try
        {
            return await responseTask;
        }
        catch (RpcException ex)
        {
            logger.LogWarning("gRPC {Method} failed with {StatusCode}: {Detail}", methodName, ex.StatusCode, ex.Status.Detail);
            throw;
        }
    }
}
