namespace Booking.Application.Common.Interfaces.MessageBroker;

/// <summary>Publishes integration events to the other services through RabbitMQ (outbox: sent after SaveChanges).</summary>
public interface IEventPublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class;

    Task PublishManyAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken)
        where TMessage : class;
}
