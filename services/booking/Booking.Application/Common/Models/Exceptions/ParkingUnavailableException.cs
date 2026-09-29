namespace Booking.Application.Common.Models.Exceptions;

public class ParkingUnavailableException(string message, Exception innerException) : ServiceUnavailableException(message, innerException);
