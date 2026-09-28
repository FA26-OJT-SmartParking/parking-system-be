using Parking.Api;

namespace Parking.Tests;

public class SlotTopicTests
{
    [Fact]
    public void TryParse_ValidTopic_ReturnsLotAndSlot()
    {
        var lotId = Guid.NewGuid();

        var parsed = SlotTopic.TryParse($"lot/{lotId}/slot/A-01", out var parsedLotId, out var slotCode);

        Assert.True(parsed);
        Assert.Equal(lotId, parsedLotId);
        Assert.Equal("A-01", slotCode);
    }

    [Theory]
    [InlineData("lot/not-a-guid/slot/A-01")]
    [InlineData("lot/00000000-0000-0000-0000-000000000001/gate/in")]
    [InlineData("lot/00000000-0000-0000-0000-000000000001/slot/")]
    [InlineData("parking/00000000-0000-0000-0000-000000000001/slot/A-01")]
    public void TryParse_InvalidTopic_ReturnsFalse(string topic)
    {
        Assert.False(SlotTopic.TryParse(topic, out _, out _));
    }
}
