using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Models.ViewModels;

namespace Kaaiman_reizen.Extensions;

public static class JourneyExtensions
{
    public static JourneyViewModel ToViewModel(this Journey journey)
    {
        return new JourneyViewModel
        {
            Id = journey.Id,
            Country = journey.Country,
            Start = journey.Start,
            End = journey.End,
            Busses = journey.Busses,
            Travelers = journey.Travelers,
            TravelLeaders = journey.TravelLeaders?.Select(l => l.ToViewModel()).ToList() ?? []
        };
    }

    public static IReadOnlyList<JourneyViewModel> ToViewModels(this IEnumerable<Journey> journeys) =>
        journeys.Select(ToViewModel).ToList();
}
