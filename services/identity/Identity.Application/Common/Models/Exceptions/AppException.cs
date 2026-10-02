namespace Identity.Application.Common.Models.Exceptions;

/// <summary>
/// An error that is answered to the client with its own HTTP status. To add an error type, derive from this class:
/// the WebAPI error handler maps it without any change.
/// </summary>
public abstract class AppException(string message, int statusCode, Exception? innerException = null) : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
}
