using Identity.Application;
using Identity.Application.Usecase.SampleUser;
using Identity.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.WebAPI.Controllers;

[Route("api/identity/users")]
public class UserController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>The sample user, built in the Persistence layer without a database call.</summary>
    [HttpGet("sample")]
    public async Task<IActionResult> GetSample([FromQuery] string? name, CancellationToken cancellationToken)
    {
        var user = await Mediator.Send(new GetSampleUserQuery(name), cancellationToken);
        return Success(user, Resources.SampleUserRetrieved);
    }
}
