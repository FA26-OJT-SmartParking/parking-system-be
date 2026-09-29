using Booking.Application.Common.Behaviors;
using Booking.Application.Usecase.Availability;
using FluentValidation;
using MediatR;

namespace Booking.Tests;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_EmptyLotId_ThrowsValidationExceptionWithTheTemplateMessage()
    {
        var behavior = new ValidationBehavior<GetLotAvailabilityQuery, string>([new GetLotAvailabilityQueryValidator()]);

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new GetLotAvailabilityQuery(Guid.Empty), _ => Task.FromResult("handler ran"), CancellationToken.None));

        Assert.Equal("lotId is missing.", error.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Handle_ValidRequest_RunsTheHandler()
    {
        var behavior = new ValidationBehavior<GetLotAvailabilityQuery, string>([new GetLotAvailabilityQueryValidator()]);

        var result = await behavior.Handle(new GetLotAvailabilityQuery(Guid.NewGuid()), _ => Task.FromResult("handler ran"), CancellationToken.None);

        Assert.Equal("handler ran", result);
    }

    [Fact]
    public async Task Handle_NoValidators_RunsTheHandler()
    {
        var behavior = new ValidationBehavior<GetLotAvailabilityQuery, string>([]);

        var result = await behavior.Handle(new GetLotAvailabilityQuery(Guid.Empty), _ => Task.FromResult("handler ran"), CancellationToken.None);

        Assert.Equal("handler ran", result);
    }
}
