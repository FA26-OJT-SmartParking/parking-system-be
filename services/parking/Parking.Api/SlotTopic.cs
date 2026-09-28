namespace Parking.Api;

/// <summary>MQTT topic used by zone cameras: <c>lot/{lotId}/slot/{slotCode}</c>.</summary>
public static class SlotTopic
{
    public const string Filter = "lot/+/slot/+";

    public static bool TryParse(string topic, out Guid lotId, out string slotCode)
    {
        lotId = Guid.Empty;
        slotCode = "";
        var parts = topic.Split('/');
        if (parts is not ["lot", var lot, "slot", var slot] || slot.Length == 0 || !Guid.TryParse(lot, out lotId))
        {
            return false;
        }

        slotCode = slot;
        return true;
    }
}
