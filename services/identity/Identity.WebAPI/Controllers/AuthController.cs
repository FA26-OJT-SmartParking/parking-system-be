using Identity.Application;
using Identity.Application.Common.Mappers;
using Identity.Application.DTOs;
using Identity.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.WebAPI.Controllers;

[Route("api/auth")]
public class AuthController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>Signs in with a user name and a password (API Design: POST /api/auth/login).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] LoginDto? loginDto, CancellationToken cancellationToken)
    {
        // An empty body is reported like missing fields ("userName is missing.")
        var session = await Mediator.Send((loginDto ?? new LoginDto()).ToCommand(), cancellationToken);
        return Success(session, Resources.SignInSuccessfully);
    }
}
