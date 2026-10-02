namespace Payment.Application.Common.Models.Exceptions;

/// <summary>The thing the request asked for does not exist. Answered with 404.</summary>
public class NotFoundException(string message) : AppException(message, 404);
