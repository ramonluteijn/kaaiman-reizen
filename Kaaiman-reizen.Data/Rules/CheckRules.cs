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
            HasExperience: windows.All(j => HasExperience.Check(RequiredExperience, journey.Country, leader.AmountOfTrips)),
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

        var rules = new List<(bool Condition, string Reason)>
        {
            (result.NoOverlap, "Deze reisleider is al ingepland op een overlappende reis."),
            (result.HasMinimumGap, $"Deze reisleider moet minimaal {MinimumGapDays} dagen tussen reizen hebben."),
            (!result.MinMaxResult.ExceedsMaxAfterAssignment, "Deze reisleider zit aan het maximum aantal reizen."),
            (result.HasExperience, "Deze reisleider heeft onvoldoende ervaring voor deze bestemming.")
        };

        foreach (var rule in rules.Where(rule => !rule.Condition))
        {
            reason = rule.Reason;
            return false;
        }

        reason = null;
        return true;
    }
}