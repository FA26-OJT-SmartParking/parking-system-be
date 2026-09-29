using Microsoft.AspNetCore.SignalR;

namespace Parking.Api;

/// <summary>Pushes slot status changes to the 3D map. Clients only listen, so the hub has no methods.</summary>
public class ParkingHub : Hub
{
}
