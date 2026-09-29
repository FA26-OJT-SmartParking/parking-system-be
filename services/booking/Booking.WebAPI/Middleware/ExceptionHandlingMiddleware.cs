namespace Booking.WebAPI.Middleware;

/// <summary>Catches every exception of a request and answers with the standard error body.</summary>
public class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, body) = ErrorExceptionHandler.HandleException(exception);
            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception while handling {Path}", context.Request.Path);
            }
            else
            {
                logger.LogWarning("Request {Path} failed with {StatusCode}: {Message}", context.Request.Path, statusCode, body.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(body);
        }
    }
}
