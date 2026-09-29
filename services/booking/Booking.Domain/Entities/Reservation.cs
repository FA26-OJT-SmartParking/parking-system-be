using Booking.Domain.Enum;

namespace Booking.Domain.Entities;

public sealed class Reservation
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid LotId { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.PendingDeposit;

    private Reservation() { }

    public Reservation(Guid userId, Guid lotId)
    {
        UserId = userId;
        LotId = lotId;
    }

    public void Confirm() => Status = ReservationStatus.Confirmed;
    public void Cancel() => Status = ReservationStatus.Cancelled;
    public void Complete() => Status = ReservationStatus.Completed;
}
