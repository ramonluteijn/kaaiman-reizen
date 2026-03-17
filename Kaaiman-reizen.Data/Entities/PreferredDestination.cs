namespace Kaaiman_reizen.Data.Entities;

/// <summary>Voorkeursbestemming (top 3) van een reisleider. Rank = 1 is hoogste voorkeur.</summary>
public class PreferredDestination
{
    public int Id { get; set; }
    public int ReisleiderId { get; set; }
    public int Rank { get; set; }  // 1, 2 of 3
    public string Destination { get; set; } = string.Empty;

    public Reisleider Reisleider { get; set; } = null!;
}
