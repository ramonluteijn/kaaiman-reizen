namespace Kaaiman_reizen.Data.Entities;

public class JourneyTravelLeader
{
    public int JourneyId { get; set; }

    public int TravelLeaderId { get; set; }

    public Journey Journey { get; set; } = null!;

    public TravelLeader TravelLeader { get; set; } = null!;
}