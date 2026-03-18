namespace Kaaiman_reizen.Data.Entities;

public class AvailabilityPeriod
{
    public int Id { get; set; }
    public int TravelLeaderId { get; set; }

    // Use DateOnly to represent dates without time. EF Core supports DateOnly in recent versions.
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }

    public TravelLeader TravelLeader { get; set; } = null!;
}
