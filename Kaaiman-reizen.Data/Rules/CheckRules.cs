using Kaaiman_reizen.Data.Entities;
namespace Kaaiman_reizen.Data.Rules;

public static class CheckRules
{
    public sealed record JourneyWindow(DateOnly Start, DateOnly End);
    public sealed record RuleSettings(
        bool NoOverlapActive,
        bool MinimumGapDaysActive,
        int MinimumGapDays,
        bool RequiredExperienceActive,
        int RequiredExperience,
        bool MinMaxJourneysActive
    );

    public static RuleSettings GetDefaultSettings() => new(
        NoOverlapActive: true,
        MinimumGapDaysActive: true,
        MinimumGapDays: RuleKeys.DefaultMinimumGapDays,
        RequiredExperienceActive: true,
        RequiredExperience: RuleKeys.DefaultRequiredExperience,
        MinMaxJourneysActive: true
    );

    public static RuleSettings FromRules(IEnumerable<Rule>? rules)
    {
        var settings = GetDefaultSettings();
        if (rules is null)
        {
            return settings;
        }

        var byKey = rules
            .GroupBy(rule => rule.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return new RuleSettings(
            NoOverlapActive: GetIsActive(byKey, RuleKeys.NoOverlap, settings.NoOverlapActive),
            MinimumGapDaysActive: GetIsActive(byKey, RuleKeys.MinimumGapDays, settings.MinimumGapDaysActive),
            MinimumGapDays: GetIntValue(byKey, RuleKeys.MinimumGapDays, settings.MinimumGapDays),
            RequiredExperienceActive: GetIsActive(byKey, RuleKeys.RequiredExperience, settings.RequiredExperienceActive),
            RequiredExperience: GetIntValue(byKey, RuleKeys.RequiredExperience, settings.RequiredExperience),
            MinMaxJourneysActive: GetIsActive(byKey, RuleKeys.MinMaxJourneys, settings.MinMaxJourneysActive)
        );
    }

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
        TravelLeader leader,
        RuleSettings? settings = null
    )
    {
        var effectiveSettings = settings ?? GetDefaultSettings();
        var windows = existingJourneys.ToList();
        var minMax = effectiveSettings.MinMaxJourneysActive
            ? MinMaxJourneys.Evaluate(windows.Count, leader.MinTrips, leader.MaxTrips)
            : new MinMaxJourneysResult(
                IsWithinLimitsAfterAssignment: true,
                BelowMinAfterAssignment: false,
                ExceedsMaxAfterAssignment: false,
                DistanceFromMinMax: 0);

        return new PlannerRuleResult(
            NoOverlap: !effectiveSettings.NoOverlapActive || !windows.Any(j => JourneysOverlap.Check(j.Start, j.End, journey.Start, journey.End)),
            HasMinimumGap: !effectiveSettings.MinimumGapDaysActive || windows.All(j => HasMinimumGapDays.Check(j.Start, j.End, journey.Start, journey.End, effectiveSettings.MinimumGapDays)),
            HasExperience: !effectiveSettings.RequiredExperienceActive || HasExperience.Check(effectiveSettings.RequiredExperience, journey.Name, leader.AmountOfTrips),
            MinMaxResult: minMax
        );
    }

    public static bool CanAssignForPlanner(
        IEnumerable<JourneyWindow> existingJourneys,
        Journey journey,
        TravelLeader leader,
        out string? reason,
        RuleSettings? settings = null)
    {
        var effectiveSettings = settings ?? GetDefaultSettings();
        var journeyWindows = existingJourneys as JourneyWindow[] ?? existingJourneys.ToArray();
        var result = EvaluateForPlanner(journeyWindows, journey, leader, effectiveSettings);

        var rules = new List<(bool Condition, string Reason)>
        {
            (result.NoOverlap, "Deze reisleider is al ingepland op een overlappende reis."),
            (result.HasMinimumGap, $"Deze reisleider moet minimaal {effectiveSettings.MinimumGapDays} dagen tussen reizen hebben."),
            (!result.MinMaxResult.ExceedsMaxAfterAssignment, $"Deze reisleider heeft een aangegeven minimum en maximum van {leader.MinTrips} en {leader.MaxTrips} reizen, momenteel gepland op {journeyWindows.Count()} reizen."),
            (result.HasExperience, $"Deze reisleider heeft onvoldoende ervaring voor deze bestemming (min {effectiveSettings.RequiredExperience} reizen).")
        };

        foreach (var rule in rules.Where(rule => !rule.Condition))
        {
            reason = rule.Reason;
            return false;
        }

        reason = null;
        return true;
    }

    private static bool GetIsActive(IReadOnlyDictionary<string, Rule> byKey, string key, bool fallback)
    {
        return byKey.TryGetValue(key, out var rule) ? rule.IsActive : fallback;
    }

    private static int GetIntValue(IReadOnlyDictionary<string, Rule> byKey, string key, int fallback)
    {
        if (!byKey.TryGetValue(key, out var rule))
        {
            return fallback;
        }

        return rule.TypedValue switch
        {
            int intValue => intValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => fallback
        };
    }
}