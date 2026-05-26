namespace Kaaiman_reizen.Data.Entities;

public class TravelLeaderAvailabilityHistory
{
    public int Id { get; set; }
    public int TravelLeaderId { get; set; }
    public int? JourneyId { get; set; }
    public int Rank { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int? PlanningVersionId { get; set; }

    public TravelLeader TravelLeader { get; set; } = null!;
    public PlanningVersion? PlanningVersion { get; set; }
    public Journey? Journey { get; set; }
}
