using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using ParkingSystem.Contracts;

using Parking.WebAPI.Hubs;
using Parking.Application.Interfaces;
using Parking.Infrastructure.Mqtt;
using Parking.Infrastructure.Persistence;

namespace Parking.WebAPI.Workers;

/// <summary>
/// Reads zone-camera events from the RabbitMQ MQTT plugin, publishes <see cref="SlotStatusChanged"/>
/// through the outbox and pushes the change to the 3D map.
/// </summary>
public class MqttSlotListener(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    IHubContext<ParkingHub> hub,
    ILogger<MqttSlotListener> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += e => HandleAsync(e.ApplicationMessage, stoppingToken);

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(configuration["Mqtt:Host"] ?? "localhost", 1883)
            .WithCredentials(configuration["Mqtt:Username"] ?? "guest", configuration["Mqtt:Password"] ?? "guest")
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
        if (!SlotTopic.TryParse(message.Topic, out var lotId, out var slotCode))
        {
            logger.LogWarning("Ignoring MQTT message on unexpected topic {Topic}", message.Topic);
            return;
        }

        SlotReading? reading;
        try
        {
            reading = JsonSerializer.Deserialize<SlotReading>(message.ConvertPayloadToString(), Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ignoring malformed payload on {Topic}", message.Topic);
            return;
        }
        if (reading is null)
        {
            return;
        }

        var change = new SlotStatusChanged(lotId, slotCode, reading.Status, reading.At);

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISlotStateStore>()
            .UpsertAsync(lotId, slotCode, reading.Status, reading.At, cancellationToken);
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(change, cancellationToken);
        // One transaction: the slot state and the outbox message are saved together, then the bus sends the message to RabbitMQ.
        await scope.ServiceProvider.GetRequiredService<ParkingDb>().SaveChangesAsync(cancellationToken);

        await hub.Clients.All.SendAsync("slotStatusChanged", change, cancellationToken);
    }

    /// <summary>Payload sent by a zone camera or the simulator.</summary>
    private record SlotReading(string Status, DateTimeOffset At);
}
