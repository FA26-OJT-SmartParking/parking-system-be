using Parking.Infrastructure.Mqtt;

namespace Parking.Tests;

public class SlotMessageParserTests
{
    private const string LotId = "00000000-0000-0000-0000-000000000001";

    [Fact]
    public void TryParse_ValidMessage_ReturnsTheCommand()
    {
        var parsed = SlotMessageParser.TryParse(
            $"lot/{LotId}/slot/A-01",
            "{\"status\":\"Occupied\",\"confidence\":0.93,\"at\":\"2026-10-02T08:00:00+00:00\"}",
            out var command, out var error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.NotNull(command);
        Assert.Equal(Guid.Parse(LotId), command.LotId);
        Assert.Equal("A-01", command.SlotCode);
        Assert.Equal("Occupied", command.Status);
        Assert.Equal(new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero), command.At);
    }

    [Fact]
    public void TryParse_UnexpectedTopic_ReturnsFalse()
    {
        var parsed = SlotMessageParser.TryParse("lot/not-a-guid/slot/A-01", "{}", out var command, out var error);

        Assert.False(parsed);
        Assert.Null(command);
        Assert.Equal("unexpected topic", error);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"status\":")]
    [InlineData("{\"status\":\"Occupied\",\"at\":\"yesterday\"}")]
    public void TryParse_MalformedPayload_ReturnsFalse(string payload)
    {
        var parsed = SlotMessageParser.TryParse($"lot/{LotId}/slot/A-01", payload, out var command, out var error);

        Assert.False(parsed);
        Assert.Null(command);
        Assert.StartsWith("malformed payload", error);
    }

    [Fact]
    public void TryParse_JsonNull_ReturnsFalse()
    {
        var parsed = SlotMessageParser.TryParse($"lot/{LotId}/slot/A-01", "null", out _, out var error);

        Assert.False(parsed);
        Assert.Equal("empty payload", error);
    }

    [Fact]
    public void TryParse_StatusMissing_ReturnsACommandForTheValidatorToReject()
    {
        var parsed = SlotMessageParser.TryParse($"lot/{LotId}/slot/A-01", "{}", out var command, out _);

        Assert.True(parsed);
        Assert.NotNull(command);
        Assert.Null(command.Status);
    }
}
