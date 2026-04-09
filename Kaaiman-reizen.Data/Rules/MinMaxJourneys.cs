namespace Kaaiman_reizen.Data.Rules;

public sealed record MinMaxJourneysResult(
    bool IsWithinLimitsAfterAssignment,
    bool BelowMinAfterAssignment,
    bool ExceedsMaxAfterAssignment,
    int DistanceFromMinMax
);

public static class MinMaxJourneys
{
    public static MinMaxJourneysResult Evaluate(int currentTripCount, int? minTrips, int? maxTrips)
    {
        var projectedTripCount = currentTripCount + 1;
        var min = minTrips ?? 0;
        var max = maxTrips ?? int.MaxValue;
     
        return new MinMaxJourneysResult(
            IsWithinLimitsAfterAssignment: projectedTripCount >= min && projectedTripCount <= max,
            BelowMinAfterAssignment: minTrips.HasValue && projectedTripCount <= minTrips.Value,
            ExceedsMaxAfterAssignment: maxTrips.HasValue && projectedTripCount > maxTrips.Value,
            DistanceFromMinMax: Math.Min(Math.Abs(projectedTripCount - min), Math.Abs(max - projectedTripCount))
        );
    }
}
