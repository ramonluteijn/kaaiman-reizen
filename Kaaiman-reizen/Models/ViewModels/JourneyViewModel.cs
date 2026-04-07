namespace Kaaiman_reizen.Models.ViewModels;

public sealed class JourneyViewModel
{
    public int Id { get; init; }
    public string Country { get; init; } = string.Empty;
    public DateOnly Start { get; init; }
    public DateOnly End { get; init; }
    public int? Busses { get; init; }
    public int? Travelers { get; init; }
    public List<TravelLeaderViewModel> TravelLeaders { get; init; } = [];
}
