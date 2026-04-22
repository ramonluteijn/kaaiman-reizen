using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Models.ViewModels;

namespace Kaaiman_reizen.Extensions;

public static class TravelLeaderExtensions
{
    public static TravelLeaderViewModel ToViewModel(this TravelLeader leader)
    {
        var periods = leader.AvailabilityPeriods ?? new List<AvailabilityPeriod>();
        var availability = periods.Count == 0
            ? "-"
            : string.Join("; ", periods
                .OrderBy(p => p.Start)
                .Select(p => $"{p.Start:dd MMM} - {p.End:dd MMM}"));

        return new TravelLeaderViewModel
        {
            Id = leader.Id,
            Name = leader.Name,
            PhoneNumber = leader.PhoneNumber,
            AmountOfTrips = leader.AmountOfTrips,
            MinTrips = leader.MinTrips,
            MaxTrips = leader.MaxTrips,
            IsActive = leader.IsActive,
            Availability = availability,
            JourneyHistory = leader.Journeys?
                .OrderByDescending(j => j.End)
                .ThenByDescending(j => j.Start)
                .Select(j => new TravelHistoryItemViewModel
                {
                    JourneyId = j.Id,
                    JourneyName = j.Name,
                    Start = j.Start,
                    End = j.End
                })
                .ToList() ?? []
        };
    }

    public static IReadOnlyList<TravelLeaderViewModel> ToViewModels(this IEnumerable<TravelLeader> leaders) =>
        leaders.Select(ToViewModel).ToList();
}
