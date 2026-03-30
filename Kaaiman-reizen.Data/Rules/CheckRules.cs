namespace Kaaiman_reizen.Data.Rules;

public static class CheckRules
{
    public sealed record JourneyWindow(DateTime Start, DateTime End);
    
    private const int MinimumGapDays = 3;


    public sealed record PlannerRuleResult(
        bool NoOverlap,
        bool HasMinimumGap,
        MinMaxJourneysResult MinMaxResult)
    {
        public bool IsEligible => NoOverlap && HasMinimumGap && !MinMaxResult.ExceedsMaxAfterAssignment && MinMaxResult.IsWithinLimitsAfterAssignment;
    }

    public static PlannerRuleResult EvaluateForPlanner(
        IEnumerable<JourneyWindow> existingJourneys,
        DateTime candidateStart,
        DateTime candidateEnd,
        int? minTrips,
        int? maxTrips)
    {
        var windows = existingJourneys.ToList();
        
        return new PlannerRuleResult(
            NoOverlap: !windows.Any(j => JourneysOverlap.Check(j.Start, j.End, candidateStart, candidateEnd)),
            HasMinimumGap: windows.All(j => HasMinimumGapDays.Check(j.Start, j.End, candidateStart, candidateEnd, MinimumGapDays)),
            MinMaxResult: MinMaxJourneys.Evaluate(windows.Count, minTrips, maxTrips)
        );
    }

    public static bool CanAssignForPlanner(
        IEnumerable<JourneyWindow> existingJourneys,
        DateTime candidateStart,
        DateTime candidateEnd,
        int? minTrips,
        int? maxTrips,
        out string? reason)
    {
        var result = EvaluateForPlanner(existingJourneys, candidateStart, candidateEnd, minTrips, maxTrips);

        if (!result.NoOverlap)
        {
            reason = "Deze reisleider is al ingepland op een overlappende reis.";
            return false;
        }

        if (!result.HasMinimumGap)
        {
            reason = $"Deze reisleider moet minimaal {MinimumGapDays} dagen tussen reizen hebben.";
            return false;
        }

        if (result.MinMaxResult.ExceedsMaxAfterAssignment)
        {
            reason = "Deze reisleider zit aan het maximum aantal reizen.";
            return false;
        }

        reason = null;
        return true;
    }
}