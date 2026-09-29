namespace Booking.Application.Exceptions;

public class ParkingUnavailableException(string message, Exception innerException) : Exception(message, innerException);
