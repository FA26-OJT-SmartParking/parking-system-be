using FluentValidation;

namespace Parking.Application.Usecase.UpdateSlotStatus;

public class UpdateSlotStatusCommandValidator : AbstractValidator<UpdateSlotStatusCommand>
{
    public UpdateSlotStatusCommandValidator()
    {
        RuleFor(command => command.LotId)
            .NotEmpty()
            .WithMessage(Resources.LotIdIsRequired);

        RuleFor(command => command.SlotCode)
            .NotEmpty()
            .WithMessage(Resources.SlotCodeIsRequired);

        RuleFor(command => command.Status)
            .NotEmpty()
            .WithMessage(Resources.StatusIsRequired);
    }
}
