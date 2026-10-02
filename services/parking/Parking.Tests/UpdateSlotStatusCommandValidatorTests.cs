using Parking.Application.Usecase.UpdateSlotStatus;

namespace Parking.Tests;

public class UpdateSlotStatusCommandValidatorTests
{
    private static readonly Guid LotId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly UpdateSlotStatusCommandValidator validator = new();

    [Fact]
    public void Validate_CompleteCommand_Passes()
    {
        var result = validator.Validate(new UpdateSlotStatusCommand(LotId, "A-01", "Occupied", DateTimeOffset.UtcNow));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyLotId_ReportsTheMessage()
    {
        var result = validator.Validate(new UpdateSlotStatusCommand(Guid.Empty, "A-01", "Occupied", DateTimeOffset.UtcNow));

        Assert.Equal("lotId is missing.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_MissingSlotCode_ReportsTheMessage(string? slotCode)
    {
        var result = validator.Validate(new UpdateSlotStatusCommand(LotId, slotCode, "Occupied", DateTimeOffset.UtcNow));

        Assert.Equal("slotCode is missing.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_MissingStatus_ReportsTheMessage(string? status)
    {
        var result = validator.Validate(new UpdateSlotStatusCommand(LotId, "A-01", status, DateTimeOffset.UtcNow));

        Assert.Equal("status is missing.", Assert.Single(result.Errors).ErrorMessage);
    }
}
