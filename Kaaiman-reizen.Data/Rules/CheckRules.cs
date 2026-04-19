using Kaaiman_reizen.Data.Entities;
namespace Kaaiman_reizen.Data.Rules;

public static class CheckRules
{
    public sealed record JourneyWindow(DateOnly Start, DateOnly End);
    private const int MinimumGapDays = 3; // min days between journeys
    private const int RequiredExperience = 3; // min number of trips for experience requirement

    public sealed record PlannerRuleResult(
        bool NoOverlap,
        bool HasMinimumGap,
        bool HasExperience,
        MinMaxJourneysResult MinMaxResult
    )
    {
        public bool IsEligible =>
            NoOverlap &&
            HasMinimumGap &&
            HasExperience &&
            !MinMaxResult.ExceedsMaxAfterAssignment &&
            MinMaxResult.IsWithinLimitsAfterAssignment;
    }

    public static PlannerRuleResult EvaluateForPlanner(
        IEnumerable<JourneyWindow> existingJourneys,
        Journey journey,
        TravelLeader leader
    )
    {
        var windows = existingJourneys.ToList();

        return new PlannerRuleResult(
            NoOverlap: !windows.Any(j => JourneysOverlap.Check(j.Start, j.End, journey.Start, journey.End)),
            HasMinimumGap: windows.All(j => HasMinimumGapDays.Check(j.Start, j.End, journey.Start, journey.End, MinimumGapDays)),
            HasExperience: windows.All(j => HasExperience.Check(RequiredExperience, journey.Name, leader.AmountOfTrips)),
            MinMaxResult: MinMaxJourneys.Evaluate(windows.Count, leader.MinTrips, leader.MinTrips)
        );
    }

    public static bool CanAssignForPlanner(
        IEnumerable<JourneyWindow> existingJourneys,
        Journey journey,
        TravelLeader leader,
        out string? reason)
    {
        var result = EvaluateForPlanner(existingJourneys, journey, leader);

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

        if (!result.HasExperience)
        {
            reason = "Deze reisleider heeft onvoldoende ervaring voor deze bestemming.";
            return false;
        }

        reason = null;
        return true;
    }
}