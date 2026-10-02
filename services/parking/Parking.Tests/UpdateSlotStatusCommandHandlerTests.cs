using Parking.Application.Common.Interfaces.MessageBroker;
using Parking.Application.Common.Interfaces.Persistence;
using Parking.Application.Common.Interfaces.Services;
using Parking.Application.Usecase.UpdateSlotStatus;
using Parking.Domain.Entities;
using ParkingSystem.Contracts;

namespace Parking.Tests;

public class UpdateSlotStatusCommandHandlerTests
{
    private static readonly Guid LotId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset At = new(2026, 10, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ValidCommand_StoresPublishesSavesAndThenNotifies()
    {
        var calls = new List<string>();
        var handler = CreateHandler(calls, out var published, out var notified);

        await handler.Handle(new UpdateSlotStatusCommand(LotId, "A-01", "Occupied", At), CancellationToken.None);

        Assert.Equal(["upsert A-01 Occupied", "publish", "save", "notify"], calls);
        var expected = new SlotStatusChanged(LotId, "A-01", "Occupied", At);
        Assert.Equal(expected, Assert.Single(published));
        Assert.Equal(expected, Assert.Single(notified));
    }

    [Fact]
    public async Task Handle_SaveFails_DoesNotNotifyTheMap()
    {
        var calls = new List<string>();
        var handler = CreateHandler(calls, out _, out var notified, failOnSave: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UpdateSlotStatusCommand(LotId, "A-01", "Available", At), CancellationToken.None));

        Assert.Equal(["upsert A-01 Available", "publish", "save"], calls);
        Assert.Empty(notified);
    }

    private static UpdateSlotStatusCommandHandler CreateHandler(
        List<string> calls, out List<object> published, out List<SlotStatusChanged> notified, bool failOnSave = false)
    {
        published = [];
        notified = [];
        return new UpdateSlotStatusCommandHandler(
            new FakeSlotStateStore(calls),
            new FakeEventPublisher(calls, published),
            new FakeUnitOfWork(calls, failOnSave),
            new FakeNotifier(calls, notified));
    }

    private sealed class FakeSlotStateStore(List<string> calls) : ISlotStateStore
    {
        public Task UpsertAsync(Guid lotId, string code, string status, DateTimeOffset at, CancellationToken cancellationToken)
        {
            calls.Add($"upsert {code} {status}");
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SlotState>> GetByLotAsync(Guid lotId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SlotState>>([]);
    }

    private sealed class FakeEventPublisher(List<string> calls, List<object> published) : IEventPublisher
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
            where TMessage : class
        {
            calls.Add("publish");
            published.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishManyAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken)
            where TMessage : class => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork(List<string> calls, bool fail) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            calls.Add("save");
            return fail ? throw new InvalidOperationException("save failed") : Task.FromResult(1);
        }
    }

    private sealed class FakeNotifier(List<string> calls, List<SlotStatusChanged> notified) : ISlotStatusNotifier
    {
        public Task NotifyAsync(SlotStatusChanged change, CancellationToken cancellationToken)
        {
            calls.Add("notify");
            notified.Add(change);
            return Task.CompletedTask;
        }
    }
}
