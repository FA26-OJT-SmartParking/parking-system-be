using Microsoft.AspNetCore.SignalR;
using Parking.Application.Common.Interfaces.Services;
using ParkingSystem.Contracts;

namespace Parking.WebAPI.Hubs;

/// <summary>Pushes a slot change to every client connected to <see cref="ParkingHub"/>.</summary>
public class SlotStatusNotifier(IHubContext<ParkingHub> hub) : ISlotStatusNotifier
{
    public Task NotifyAsync(SlotStatusChanged change, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("slotStatusChanged", change, cancellationToken);
}
