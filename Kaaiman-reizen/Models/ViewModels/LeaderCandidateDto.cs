namespace Kaaiman_reizen.Models.ViewModels;

using Data.Rules;

public sealed record LeaderCandidateDto(
    TravelLeaderViewModel Leader,
    bool NoOverlap,
    bool HasThreeDayBuffer,
    int CurrentTripCount,
    MinMaxJourneysResult MinMaxResult
);
