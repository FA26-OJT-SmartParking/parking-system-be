namespace Booking.Application.Exceptions;

/// <summary>The other service does not implement this call yet (it answered gRPC Unimplemented).</summary>
public class RemoteCallNotImplementedException(string message, Exception innerException) : Exception(message, innerException);
