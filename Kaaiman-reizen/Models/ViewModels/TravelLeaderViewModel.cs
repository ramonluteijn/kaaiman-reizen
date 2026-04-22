using Kaaiman_reizen.Data.Entities;
namespace Kaaiman_reizen.Models.ViewModels;

public sealed class TravelLeaderViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int? AmountOfTrips { get; init; }
    public int? MinTrips { get; init; }
    public int? MaxTrips { get; init; }
    public bool IsActive { get; init; }
    public string Availability { get; init; } = "-";
    public int YearsOfExperience { get; set; }

    // Key = Country/Destination string, Value = Rank (1, 2, or 3)
    public Dictionary<string, int> PreferredDestinations { get; set; } = new();

    public List<TravelHistoryItemViewModel> JourneyHistory { get; init; } = [];

    public TravelLeader ToEntity()
    {
        return new TravelLeader
        {
            Id = this.Id,
            Name = this.Name,
            PhoneNumber = this.PhoneNumber,
            AmountOfTrips = this.AmountOfTrips,
            MinTrips = this.MinTrips,
            MaxTrips = this.MaxTrips,
            IsActive = this.IsActive,
        };
    }
}

public sealed class TravelHistoryItemViewModel
{
    public int JourneyId { get; init; }
    public string JourneyName { get; init; } = string.Empty;
    public DateOnly Start { get; init; }
    public DateOnly End { get; init; }
}
