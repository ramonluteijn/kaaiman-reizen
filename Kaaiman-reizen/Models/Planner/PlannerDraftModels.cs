// <summary>
// Models for the planner draft request and result.
// The reason for this is that we do not need all the data from the database models for the planner.
// </summary>

namespace Kaaiman_reizen.Models.Planner;

public class PlannerDraftRequest
{
    public List<PlannerLeaderInput> Leaders { get; set; } = [];
    public List<PlannerLeaderInput> AllActiveLeaders { get; set; } = [];
    public List<PlannerJourneyInput> Journeys { get; set; } = [];
}

public class PlannerLeaderInput
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public int AmountOfTrips { get; set; }
    public int MinTrips { get; set; }
    public int MaxTrips { get; set; }
    public Dictionary<int, int> PreferredDestinations { get; set; } = [];
}

public class PlannerJourneyInput
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    /// <summary>How many leaders the solver must assign to this journey.</summary>
    public int RequiredLeaders { get; set; } = 1;
}

public class PlannerDraftResult
{
    public bool IsSuccess { get; set; }

    /// <summary>Set only if (no leaders / no journeys configured).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Maps journey ID warning message for journeys that could not be planned.</summary>
    public Dictionary<int, string> JourneyWarnings { get; set; } = [];

    /// <summary>Maps journey ID list of all assigned leaders (may be more than one).</summary>
    public Dictionary<int, List<JourneyAssignmentResult>> JourneyAssignments { get; set; } = [];

    public int Rank1Matches => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == 1);
    public int Rank2Matches => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == 2);
    public int Rank3Matches => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == 3);
    public int NoPreferenceMatches => JourneyAssignments.Values.SelectMany(l => l).Count(a => a.RankMatched == null);
}

public class JourneyAssignmentResult
{
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public int? RankMatched { get; set; }
}
