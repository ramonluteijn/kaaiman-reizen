namespace Kaaiman_reizen.Data.Entities;

public class TravelLeader
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int AmountOfTrips { get; set; }
    public string WhenAvailable { get; set; } = string.Empty;

    /// <summary>Top 3 voorkeursbestemmingen (op volgorde: 1 = eerste keuze).</summary>
    public List<PreferredDestination> PreferredDestinations { get; set; } = new();
}
