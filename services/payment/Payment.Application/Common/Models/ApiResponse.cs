namespace Payment.Application.Common.Models;

/// <summary>
/// Body of every API response, success or error (see the API Design Template):
/// { "result": ..., "isSuccess": ..., "statusCode": ..., "message": "..." }.
/// </summary>
public sealed record ApiResponse<T>(T? Result, bool IsSuccess, int StatusCode, string Message);

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T result, string message, int statusCode = 200) =>
        new(result, true, statusCode, message);

    public static ApiResponse<object> Failure(int statusCode, string message) =>
        new(null, false, statusCode, message);
}
