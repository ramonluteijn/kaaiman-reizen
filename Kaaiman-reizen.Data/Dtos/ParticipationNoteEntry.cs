namespace Kaaiman_reizen.Data.Dtos;

public sealed class ParticipationNoteEntry
{
    public int PlanningRoundId { get; init; }
    public string PlanningRoundName { get; init; } = string.Empty;
    public int TravelLeaderId { get; init; }
    public string TravelLeaderName { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
}
