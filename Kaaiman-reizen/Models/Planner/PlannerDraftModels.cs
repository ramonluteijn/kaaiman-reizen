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
}

public class PlannerDraftResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; } = string.Empty;

    public Dictionary<int, JourneyAssignmentResult> JourneyAssignments { get; set; } = [];

    // Computed to keep in sync with JourneyAssignments.
    public int Rank1Matches        => JourneyAssignments.Values.Count(a => a.RankMatched == 1);
    public int Rank2Matches        => JourneyAssignments.Values.Count(a => a.RankMatched == 2);
    public int Rank3Matches        => JourneyAssignments.Values.Count(a => a.RankMatched == 3);
    public int NoPreferenceMatches => JourneyAssignments.Values.Count(a => a.RankMatched == null);
}

public class JourneyAssignmentResult
{
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public int? RankMatched { get; set; }
}
