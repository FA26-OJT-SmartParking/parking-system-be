using FluentValidation;

namespace Booking.Application.Usecase.Availability;

public class GetLotAvailabilityQueryValidator : AbstractValidator<GetLotAvailabilityQuery>
{
    public GetLotAvailabilityQueryValidator()
    {
        RuleFor(query => query.LotId)
            .NotEmpty()
            .WithMessage(Resources.LotIdIsRequired);
    }
}
