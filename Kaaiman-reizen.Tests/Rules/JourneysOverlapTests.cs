using Kaaiman_reizen.Data.Rules;

namespace Kaaiman_reizen.Tests.Rules;

public class JourneysOverlapTests
{
    [Fact]
    public void Check_ReturnsTrue_WhenJourneysOverlap()
    {
        var existingStart = new DateOnly(2024, 1, 1);
        var existingEnd = new DateOnly(2024, 1, 10);
        var candidateStart = new DateOnly(2024, 1, 5);
        var candidateEnd = new DateOnly(2024, 1, 15);

        var result = JourneysOverlap.Check(existingStart, existingEnd, candidateStart, candidateEnd);
        Assert.True(result);
    }
    
    [Fact]
    public void Check_ReturnsFalse_WhenJourneysDoNotOverlap()
    {
        var existingStart = new DateOnly(2024, 1, 1);
        var existingEnd = new DateOnly(2024, 1, 10);
        var candidateStart = new DateOnly(2024, 1, 11);
        var candidateEnd = new DateOnly(2024, 1, 20);

        var result = JourneysOverlap.Check(existingStart, existingEnd, candidateStart, candidateEnd);
        Assert.False(result);
    }
}