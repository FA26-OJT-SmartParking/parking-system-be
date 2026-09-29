using Booking.Application.Exceptions;
using Booking.Application.Features.Availability;
using Microsoft.AspNetCore.Mvc;

namespace Booking.API.Controllers;

[ApiController]
[Route("lots")]
public class AvailabilityController(GetLotAvailabilityHandler handler) : ControllerBase
{
    /// <summary>Free and occupied slots of a lot. Public: guests can browse lots.</summary>
    [HttpGet("{lotId:guid}/availability")]
    public async Task<ActionResult<LotAvailability>> Get(Guid lotId, CancellationToken cancellationToken)
    {
        try
        {
            return await handler.HandleAsync(lotId, cancellationToken);
        }
        catch (ParkingUnavailableException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Parking service unavailable");
        }
    }
}
