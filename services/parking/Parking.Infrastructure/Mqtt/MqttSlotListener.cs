using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Parking.Infrastructure.Mqtt;

/// <summary>
/// Reads device events from the RabbitMQ MQTT plugin and sends each one to the UpdateSlotStatus use case.
/// It only connects, reads and forwards: saving, publishing and notifying happen in the handler.
/// </summary>
public class MqttSlotListener(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<MqttSlotListener> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += e => HandleAsync(e.ApplicationMessage, stoppingToken);

        // No defaults: a missing setting stops the service at startup instead of connecting as guest
        var host = configuration["Mqtt:Host"] ?? throw new InvalidOperationException("Mqtt:Host is not configured.");
        var username = configuration["Mqtt:Username"] ?? throw new InvalidOperationException("Mqtt:Username is not configured.");
        var password = configuration["Mqtt:Password"] ?? throw new InvalidOperationException("Mqtt:Password is not configured.");
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, 1883)
            .WithCredentials(username, password)
            .WithClientId("parking-service")
            .Build();

        // MQTTnet 5 has no managed client, so reconnect in a loop.
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!client.IsConnected)
            {
                try
                {
                    await client.ConnectAsync(options, stoppingToken);
                    await client.SubscribeAsync(SlotTopic.Filter, cancellationToken: stoppingToken);
                    logger.LogInformation("Subscribed to MQTT topic {Topic}", SlotTopic.Filter);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "MQTT connection failed, retrying in 5 seconds");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task HandleAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
    {
        if (!SlotMessageParser.TryParse(message.Topic, message.ConvertPayloadToString(), out var command, out var error))
        {
            logger.LogWarning("Ignoring MQTT message on {Topic}: {Reason}", message.Topic, error);
            return;
        }

        try
        {
            // A new scope per message: the handler, its DbContext and the outbox share it
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command!, cancellationToken);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Ignoring MQTT message on {Topic}: {Reason}", message.Topic, ex.Errors.FirstOrDefault()?.ErrorMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One bad message must not stop the listener
            logger.LogError(ex, "Could not handle the MQTT message on {Topic}", message.Topic);
        }
    }
}
