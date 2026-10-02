using Booking.Application;
using Booking.Application.Usecase.Availability;
using Booking.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.WebAPI.Controllers;

[Route("api/booking/lots")]
public class AvailabilityController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>Free and occupied slots of a lot. Public: guests can browse lots.</summary>
    [AllowAnonymous]
    [HttpGet("{lotId:guid}/availability")]
    public async Task<IActionResult> Get(Guid lotId, CancellationToken cancellationToken)
    {
        var availability = await Mediator.Send(new GetLotAvailabilityQuery(lotId), cancellationToken);
        return Success(availability, Resources.LotAvailabilityRetrieved);
    }
}
