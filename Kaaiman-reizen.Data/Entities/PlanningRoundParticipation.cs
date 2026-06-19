using Kaaiman_reizen.Data.Enum;

namespace Kaaiman_reizen.Data.Entities;

public class PlanningRoundParticipation
{
    public int Id { get; set; }
    public int PlanningRoundId { get; set; }
    public int TravelLeaderId { get; set; }
    public int? MinTrips { get; set; }
    public int? MaxTrips { get; set; }
    public string Note { get; set; } = string.Empty;
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;
    public DateTime? SubmittedAt { get; set; }

    public PlanningRound PlanningRound { get; set; } = null!;
    public TravelLeader TravelLeader { get; set; } = null!;
    public List<PlanningRoundPreference> Preferences { get; set; } = [];
}
