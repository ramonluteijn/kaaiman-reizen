using Kaaiman_reizen.Data.Entities;

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

    public Journey ToEntity()
    {
        return new Journey
        {
            Id = this.Id,
            Country = this.Country,
            Start = this.Start,
            End = this.End,
            Busses = this.Busses,
            Travelers = this.Travelers,
            TravelLeaders = this.TravelLeaders.Select(tl => tl.ToEntity()).ToList()
        };
    }
}
