using Kaaiman_reizen.Helpers;

namespace Kaaiman_reizen.Tests.Helpers;

public class DateDisplayTests
{
    [Fact]
    public void FormatDate_WithDateOnly_UsesDayMonthYearFormat()
    {
        var date = new DateOnly(2026, 11, 5);

        var result = DateDisplay.FormatDate(date);

        Assert.Equal("05-11-2026", result);
    }

    [Fact]
    public void ToUserLocal_WithUtcInput_AppliesTimezoneOffsetMinutes()
    {
        var utc = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc);

        // UTC+2 in JS getTimezoneOffset terms is -120 minutes.
        var result = DateDisplay.ToUserLocal(utc, -120);

        Assert.Equal(new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToUserLocal_WithUnspecifiedInput_TreatsValueAsUtc()
    {
        // Simulates a value materialized from a database provider as DateTimeKind.Unspecified.
        var unspecified = new DateTime(2026, 3, 10, 23, 30, 0, DateTimeKind.Unspecified);

        // UTC+2 in JS getTimezoneOffset terms is -120 minutes.
        var result = DateDisplay.ToUserLocal(unspecified, -120);

        Assert.Equal(new DateTime(2026, 3, 11, 1, 30, 0, DateTimeKind.Utc), result);
    }
}
