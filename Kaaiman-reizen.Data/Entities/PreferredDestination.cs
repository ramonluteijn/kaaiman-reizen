namespace Kaaiman_reizen.Data.Entities;

/// <summary>Voorkeursreis van een reisleider. Rank 1-3 = voorkeur (1 = sterkst), Rank 0 = beschikbaar maar geen voorkeur.</summary>
public class PreferredDestination
{
    public int Id { get; set; }
    public int TravelLeaderId { get; set; }
    public int Rank { get; set; }
    public int? JourneyId { get; set; }

    public TravelLeader TravelLeader { get; set; } = null!;
    public Journey? Journey { get; set; }
}
