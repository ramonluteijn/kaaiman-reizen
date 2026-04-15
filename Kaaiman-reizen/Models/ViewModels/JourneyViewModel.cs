using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Models.ViewModels;

public sealed class JourneyViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly Start { get; init; }
    public DateOnly End { get; init; }
    public int? Busses { get; init; }
    public int? Travelers { get; init; }
    public int RequiredLeaders { get; init; } = 1;
    public List<TravelLeaderViewModel> TravelLeaders { get; init; } = [];

    public Journey ToEntity()
    {
        return new Journey
        {
            Id              = this.Id,
            Name         = this.Name,
            Start           = this.Start,
            End             = this.End,
            Busses          = this.Busses,
            Travelers       = this.Travelers,
            RequiredLeaders = this.RequiredLeaders,
            TravelLeaders   = this.TravelLeaders.Select(tl => tl.ToEntity()).ToList()
        };
    }
}
