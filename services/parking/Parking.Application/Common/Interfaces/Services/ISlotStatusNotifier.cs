using ParkingSystem.Contracts;

namespace Parking.Application.Common.Interfaces.Services;

/// <summary>Tells the clients that watch the 3D map that a slot changed.</summary>
public interface ISlotStatusNotifier
{
    Task NotifyAsync(SlotStatusChanged change, CancellationToken cancellationToken);
}
