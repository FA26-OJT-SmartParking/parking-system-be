using FluentValidation;
using Booking.Application;
using Booking.Application.Common.Models;
using Booking.Application.Common.Models.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking.WebAPI.Middleware;

/// <summary>Turns an exception into the status code and body that the API Design Template defines.</summary>
public static class ErrorExceptionHandler
{
    public static (int StatusCode, ApiResponse<object> Body) HandleException(Exception exception) => exception switch
    {
        // The first failed rule is reported, as in the template's examples ("userName is missing.")
        ValidationException validation => Failure(StatusCodes.Status400BadRequest, validation.Errors.FirstOrDefault()?.ErrorMessage ?? Resources.CommonErrorMessage),
        NotFoundException notFound => Failure(StatusCodes.Status404NotFound, notFound.Message),
        ServiceUnavailableException unavailable => Failure(StatusCodes.Status503ServiceUnavailable, unavailable.Message),
        NotImplementedException => Failure(StatusCodes.Status501NotImplemented, Resources.NotImplementedExceptionMessage),
        DbUpdateException => Failure(StatusCodes.Status400BadRequest, Resources.DbUpdateExceptionMessage),
        // Never send the message of an unexpected exception to the client: it can contain internals
        _ => Failure(StatusCodes.Status500InternalServerError, Resources.UnexpectedErrorMessage),
    };

    /// <summary>A request body that cannot be read (bad JSON, wrong type) is answered with the same body as every other error.</summary>
    public static IActionResult InvalidModelState(ActionContext context)
    {
        var message = context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? Resources.CommonErrorMessage;
        return new ObjectResult(ApiResponse.Failure(StatusCodes.Status400BadRequest, message)) { StatusCode = StatusCodes.Status400BadRequest };
    }

    private static (int, ApiResponse<object>) Failure(int statusCode, string message) =>
        (statusCode, ApiResponse.Failure(statusCode, message));
}
