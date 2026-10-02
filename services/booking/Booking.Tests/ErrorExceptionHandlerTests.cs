using Booking.Application.Common.Models.Exceptions;
using Booking.WebAPI.Middleware;
using FluentValidation;
using FluentValidation.Results;

namespace Booking.Tests;

public class ErrorExceptionHandlerTests
{
    [Fact]
    public void HandleException_ValidationException_Is400WithTheFirstMessage()
    {
        var error = new ValidationException([new ValidationFailure("UserName", "userName is missing."), new ValidationFailure("Password", "password is missing.")]);

        var (statusCode, body) = ErrorExceptionHandler.HandleException(error);

        Assert.Equal(400, statusCode);
        Assert.Equal("userName is missing.", body.Message);
        Assert.Null(body.Result);
        Assert.False(body.IsSuccess);
        Assert.Equal(400, body.StatusCode);
    }

    [Fact]
    public void HandleException_NotFoundException_Is404()
    {
        var (statusCode, body) = ErrorExceptionHandler.HandleException(new NotFoundException("Lot not found."));

        Assert.Equal(404, statusCode);
        Assert.Equal("Lot not found.", body.Message);
    }

    [Fact]
    public void HandleException_ParkingUnavailable_Is503()
    {
        var (statusCode, body) = ErrorExceptionHandler.HandleException(new ParkingUnavailableException("Parking is down.", new Exception()));

        Assert.Equal(503, statusCode);
        Assert.Equal("Parking is down.", body.Message);
    }

    [Fact]
    public void HandleException_NewAppExceptionType_IsAnsweredWithItsOwnStatusWithoutChangingTheHandler()
    {
        var (statusCode, body) = ErrorExceptionHandler.HandleException(new ConflictException("Slot already taken."));

        Assert.Equal(409, statusCode);
        Assert.Equal(409, body.StatusCode);
        Assert.Equal("Slot already taken.", body.Message);
    }

    [Fact]
    public void HandleException_RemoteCallNotImplemented_Is501()
    {
        var (statusCode, _) = ErrorExceptionHandler.HandleException(new RemoteCallNotImplementedException("not yet", new Exception()));

        Assert.Equal(501, statusCode);
    }

    [Fact]
    public void HandleException_AnythingElse_Is500WithAGenericMessage()
    {
        var (statusCode, body) = ErrorExceptionHandler.HandleException(new Exception("password=secret"));

        Assert.Equal(500, statusCode);
        Assert.DoesNotContain("secret", body.Message);
    }

    private sealed class ConflictException(string message) : AppException(message, 409);
}
