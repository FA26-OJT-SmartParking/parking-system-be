using Parking.Application.Common.Interfaces.MessageBroker;
using MassTransit;

namespace Parking.Infrastructure.MessageBroker;

public sealed class EventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class =>
        publishEndpoint.Publish(message, cancellationToken);

    public async Task PublishManyAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken)
        where TMessage : class
    {
        foreach (var message in messages)
        {
            await publishEndpoint.Publish(message, cancellationToken);
        }
    }
}
