namespace Kaaiman_reizen.Data.Entities;

public class PlanningAssignment
{
    public int Id { get; set; }

    public int PlanningVersionId { get; set; }
    public PlanningVersion PlanningVersion { get; set; } = default!;

    public int JourneyId { get; set; }
    public Journey Journey { get; set; } = default!;

    public int TravelLeaderId { get; set; }
    public TravelLeader TravelLeader { get; set; } = default!;
}
