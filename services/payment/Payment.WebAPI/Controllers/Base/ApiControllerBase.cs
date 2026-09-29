using Payment.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Payment.WebAPI.Controllers.Base;

[ApiController]
public abstract class ApiControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    /// <summary>200 with the standard response body (result, isSuccess, statusCode, message).</summary>
    protected IActionResult Success<T>(T result, string message) => Ok(ApiResponse.Success(result, message));
}
