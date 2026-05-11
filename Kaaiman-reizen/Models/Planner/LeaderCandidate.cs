namespace Kaaiman_reizen.Models.Planner;

public record LeaderCandidate(
    int LeaderId,
    string LeaderName,
    int? PreferenceRank,
    bool IsAlreadyAssigned,
    bool HasConflict,
    string ConflictJourneyName,
    bool ExceedsMaxTrips,
    int CurrentAssignments,
    int MaxTrips,
    string? ValidationReason
);
