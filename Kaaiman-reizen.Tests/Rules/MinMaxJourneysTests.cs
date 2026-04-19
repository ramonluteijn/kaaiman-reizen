using Kaaiman_reizen.Data.Rules;

namespace Kaaiman_reizen.Tests.Rules;

public class MinMaxJourneysTests
{
    [Fact]
    public void Check_ReturnsTrue_WhenWithinLimits()
    {
        var result = MinMaxJourneys.Evaluate(currentTripCount: 2, minTrips: 1, maxTrips: 3);
        Assert.True(result.IsWithinLimitsAfterAssignment);
        Assert.False(result.BelowMinAfterAssignment);
        Assert.False(result.ExceedsMaxAfterAssignment);
        Assert.Equal(0, result.DistanceFromMinMax);
    }
    
    [Fact]
    public void Check_ReturnsFalse_WhenOutOfLimits()
    {      
        var result = MinMaxJourneys.Evaluate(currentTripCount: 3, minTrips: 1, maxTrips: 3);
        Assert.False(result.IsWithinLimitsAfterAssignment);
        Assert.False(result.BelowMinAfterAssignment);
        Assert.True(result.ExceedsMaxAfterAssignment);
        Assert.Equal(1, result.DistanceFromMinMax);
    }
}