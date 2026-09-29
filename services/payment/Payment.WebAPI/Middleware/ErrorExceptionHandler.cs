using FluentValidation;
using Payment.Application;
using Payment.Application.Common.Models;
using Payment.Application.Common.Models.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Payment.WebAPI.Middleware;

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
        var failed = context.ModelState.FirstOrDefault(entry => entry.Value?.Errors.Count > 0);
        var error = failed.Value?.Errors.FirstOrDefault();

        // Errors of the JSON body have keys such as "$.userName"; the parser message ("... LineNumber: 0 ...") means nothing to the client
        var isBodyError = failed.Key is not null && failed.Key.StartsWith('$');
        var message = error is null ? Resources.CommonErrorMessage
            : isBodyError || error.Exception is not null || string.IsNullOrWhiteSpace(error.ErrorMessage) ? Resources.InvalidRequestBodyMessage
            : error.ErrorMessage;
        return new ObjectResult(ApiResponse.Failure(StatusCodes.Status400BadRequest, message)) { StatusCode = StatusCodes.Status400BadRequest };
    }

    /// <summary>Gives an empty error response (for example 404 for an unknown route) the same body as every other error.</summary>
    public static Task WriteStatusCodeBody(StatusCodeContext context)
    {
        var statusCode = context.HttpContext.Response.StatusCode;
        var message = statusCode == StatusCodes.Status404NotFound ? Resources.NotFoundMessage : Resources.CommonErrorMessage;
        return context.HttpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(statusCode, message));
    }

    private static (int, ApiResponse<object>) Failure(int statusCode, string message) =>
        (statusCode, ApiResponse.Failure(statusCode, message));
}
