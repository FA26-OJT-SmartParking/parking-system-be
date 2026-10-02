namespace Identity.Application.Common.Models.Exceptions;

/// <summary>Another service or resource this request depends on cannot be reached. Answered with 503.</summary>
public class ServiceUnavailableException(string message, Exception? innerException = null) : AppException(message, 503, innerException);
