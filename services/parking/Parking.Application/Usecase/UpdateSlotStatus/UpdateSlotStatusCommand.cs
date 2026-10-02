using MediatR;

namespace Parking.Application.Usecase.UpdateSlotStatus;

/// <summary>A zone camera reported the status of one slot.</summary>
public record UpdateSlotStatusCommand(Guid LotId, string? SlotCode, string? Status, DateTimeOffset At) : IRequest;
