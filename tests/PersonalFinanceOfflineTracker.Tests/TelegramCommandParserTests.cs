using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

namespace PersonalFinanceOfflineTracker.Tests;

public sealed class TelegramCommandParserTests
{
    [Fact]
    public void Parse_AddCommand_WithValidInput_ReturnsParsed()
    {
        var parser = new TelegramCommandParser();
        var today = new DateOnly(2026, 2, 12);

        var result = parser.Parse("/add 120.50 \"coffee beans\" groceries", today);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParsedCommandKind.Add, result.CommandKind);
        Assert.NotNull(result.AddCommand);
        Assert.Equal(120.50m, result.AddCommand!.Amount);
        Assert.Equal("coffee beans", result.AddCommand.Description);
        Assert.Equal("groceries", result.AddCommand.Category);
        Assert.Equal(today, result.AddCommand.TransactionDate);
    }

    [Fact]
    public void Parse_AddCommand_WithInvalidAmount_ReturnsRejected()
    {
        var parser = new TelegramCommandParser();
        var today = new DateOnly(2026, 2, 12);

        var result = parser.Parse("/add abc coffee", today);

        Assert.False(result.IsSuccess);
        Assert.Equal(TelegramContract.ErrorCode.AmountInvalid, result.ErrorCode);
    }
}
