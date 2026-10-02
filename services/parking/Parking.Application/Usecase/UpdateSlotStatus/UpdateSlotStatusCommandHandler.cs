using MediatR;
using Parking.Application.Common.Interfaces.MessageBroker;
using Parking.Application.Common.Interfaces.Persistence;
using Parking.Application.Common.Interfaces.Services;
using ParkingSystem.Contracts;

namespace Parking.Application.Usecase.UpdateSlotStatus;

/// <summary>Saves the new status of a slot, announces it to the other services and to the 3D map.</summary>
public class UpdateSlotStatusCommandHandler(
    ISlotStateStore slots,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    ISlotStatusNotifier notifier) : IRequestHandler<UpdateSlotStatusCommand>
{
    public async Task Handle(UpdateSlotStatusCommand request, CancellationToken cancellationToken)
    {
        // The validator has already rejected empty values
        var change = new SlotStatusChanged(request.LotId, request.SlotCode!, request.Status!, request.At);

        await slots.UpsertAsync(change.LotId, change.SlotCode, change.Status, change.At, cancellationToken);
        await eventPublisher.PublishAsync(change, cancellationToken);

        // One transaction: the slot state and the outbox message are saved together, then the bus sends the message to RabbitMQ.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.NotifyAsync(change, cancellationToken);
    }
}
