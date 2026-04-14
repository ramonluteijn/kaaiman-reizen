using Kaaiman_reizen.Models.ViewModels;

namespace Kaaiman_reizen.Models;

public class PlannerDraftRequest
{
    // The pool of leaders the solver can pick from
    public List<TravelLeaderViewModel> AvailableLeaders { get; set; } = new();
    
    // The journeys that need to be planned
    public List<JourneyViewModel> JourneysToPlan { get; set; } = new();
}

public class JourneyAssignmentResult
{
    public int LeaderId { get; set; }
    public int? RankMatched { get; set; }
}

public class PlannerDraftResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    // KPIs
    public int Rank1Matches { get; set; }
    public int Rank2Matches { get; set; }
    public int Rank3Matches { get; set; }
    public int NoPreferenceMatches { get; set; }
    
    // Maps a Journey ID to the detailed Assignment Result
    public Dictionary<int, JourneyAssignmentResult> JourneyAssignments { get; set; } = new();
}
