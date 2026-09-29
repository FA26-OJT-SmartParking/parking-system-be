namespace Booking.Application.Common.Models.Exceptions;

/// <summary>The other service answered Unimplemented. Never treated as "no debt" or "slot assigned".</summary>
public class RemoteCallNotImplementedException(string message, Exception innerException) : NotImplementedException(message, innerException);
