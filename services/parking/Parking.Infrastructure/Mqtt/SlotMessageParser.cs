using System.Text.Json;
using Parking.Application.Usecase.UpdateSlotStatus;

namespace Parking.Infrastructure.Mqtt;

/// <summary>Turns a zone-camera message (topic and JSON payload) into the command that updates the slot.</summary>
public static class SlotMessageParser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>False, with the reason, when the topic or the payload cannot be read. Values that are missing are left to the validator.</summary>
    public static bool TryParse(string topic, string payload, out UpdateSlotStatusCommand? command, out string? error)
    {
        command = null;
        error = null;

        if (!SlotTopic.TryParse(topic, out var lotId, out var slotCode))
        {
            error = "unexpected topic";
            return false;
        }

        SlotReading? reading;
        try
        {
            reading = JsonSerializer.Deserialize<SlotReading>(payload, Json);
        }
        catch (JsonException ex)
        {
            error = $"malformed payload ({ex.Message})";
            return false;
        }

        if (reading is null)
        {
            error = "empty payload";
            return false;
        }

        command = new UpdateSlotStatusCommand(lotId, slotCode, reading.Status, reading.At);
        return true;
    }

    /// <summary>Payload sent by a zone camera or the simulator.</summary>
    private record SlotReading(string? Status, DateTimeOffset At);
}
