namespace Kaaiman_reizen.Models.ViewModels;

public sealed class JourneyViewModel
{
    public int Id { get; init; }
    public string Country { get; init; } = string.Empty;
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public int? Busses { get; init; }
    public int? Travelers { get; init; }
    public List<TravelLeaderViewModel> TravelLeaders { get; init; } = [];
}
