// <summary>
// Models for the planner draft request and result.
// The reason for this is that we do not need all the data from the database models for the planner.
// </summary>

namespace Kaaiman_reizen.Models.Planner;

public class PlannerDraftRequest
{
    public List<PlannerLeaderInput> Leaders { get; set; } = [];
    public List<PlannerJourneyInput> Journeys { get; set; } = [];
}

public class PlannerLeaderInput
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxTrips { get; set; }
    public List<(DateOnly Start, DateOnly End)> AvailabilityPeriods { get; set; } = [];
    public Dictionary<string, int> PreferredDestinations { get; set; } = [];
}

public class PlannerJourneyInput
{
    public int Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    /// <summary>How many leaders the solver must assign to this journey.</summary>
    public int RequiredLeaders { get; set; } = 1;
}

public class PlannerDraftResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; } = string.Empty;

    /// <summary>Maps journey ID → list of all assigned leaders (may be more than one).</summary>
    public Dictionary<int, List<JourneyAssignmentResult>> JourneyAssignments { get; set; } = [];

    // KPI computed props — flatten list-of-lists
    public int Rank1Matches        => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == 1);
    public int Rank2Matches        => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == 2);
    public int Rank3Matches        => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == 3);
    public int NoPreferenceMatches => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == null);
}

public class JourneyAssignmentResult
{
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public int? RankMatched { get; set; }
}
