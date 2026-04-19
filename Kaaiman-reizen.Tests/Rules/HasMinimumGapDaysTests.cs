using Kaaiman_reizen.Data.Rules;

namespace Kaaiman_reizen.Tests.Rules;

public class HasMinimumGapDaysTests
{
    [Fact]
    public void Check_ReturnsTrue_WhenGapIsSufficient()
    {
        var existingStart = new DateOnly(2024, 1, 1);
        var existingEnd = new DateOnly(2024, 1, 10);
        var candidateStart = new DateOnly(2024, 1, 20);
        var candidateEnd = new DateOnly(2024, 1, 30);
        var minimumGapDays = 5;

        var result = HasMinimumGapDays.Check(existingStart, existingEnd, candidateStart, candidateEnd, minimumGapDays);
        Assert.True(result);
    }

    [Fact]
    public void Check_ReturnsFalse_WhenGapIsTooSmall()
    {
        var existingStart = new DateOnly(2024, 1, 1);
        var existingEnd = new DateOnly(2024, 1, 10);
        var candidateStart = new DateOnly(2024, 1, 12);
        var candidateEnd = new DateOnly(2024, 1, 18);
        var minimumGapDays = 5;

        var result = HasMinimumGapDays.Check(existingStart, existingEnd, candidateStart, candidateEnd, minimumGapDays);
        Assert.False(result);
    }

    [Fact]
    public void Check_ReturnsFalse_WhenJourneysOverlap()
    {
        var existingStart = new DateOnly(2024, 1, 1);
        var existingEnd = new DateOnly(2024, 1, 10);
        var candidateStart = new DateOnly(2024, 1, 5);
        var candidateEnd = new DateOnly(2024, 1, 15);
        var minimumGapDays = 5;

        var result = HasMinimumGapDays.Check(existingStart, existingEnd, candidateStart, candidateEnd, minimumGapDays);
        Assert.False(result);
    }

    [Fact]
    public void Check_ReturnsTrue_WhenGapEqualsMinimum()
    {
        var existingStart = new DateOnly(2024, 1, 1);
        var existingEnd = new DateOnly(2024, 1, 10);
        var candidateStart = new DateOnly(2024, 1, 15);
        var candidateEnd = new DateOnly(2024, 1, 20);
        var minimumGapDays = 5;

        var result = HasMinimumGapDays.Check(existingStart, existingEnd, candidateStart, candidateEnd, minimumGapDays);
        Assert.True(result);
    }
}