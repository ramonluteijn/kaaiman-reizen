using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Calendar;

public partial class TravelLeaderCalendar : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Parameter]
    public int? Id { get; set; }

    private TravelLeader _leader = new();
    private bool _loading = true;
    private bool _notFound = false;
    protected List<Journey> _journeys = [];

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _notFound = false;

        TravelLeader? item = null;
        if (Id.HasValue && Id.Value > 0)
        {
            item = await LeaderService.GetTravelLeaderByIdAsync(Id.Value);
        }
        else
        {
            var all = await LeaderService.GetTravelLeadersAsync();
            item = all.FirstOrDefault();
            if (item != null)
                Id = item.Id;
        }

        if (item == null)
        {
            _notFound = true;
            _loading = false;
            return;
        }

        _leader = item;

        _journeys = (_leader.Journeys ?? [])
           .OrderByDescending(journey => journey.End)
           .ThenByDescending(journey => journey.Start)
           .ToList();

        _loading = false;
    }


    private IReadOnlyList<JourneyViewModel> GetCalendarJourneys()
    {
        return _leader.Journeys.Select(j =>
        {
            var leaders = new List<TravelLeaderViewModel>();
            foreach (var t in j.TravelLeaders)
                leaders.Add(new TravelLeaderViewModel { Id = t.Id, Name = t.Name });

            return new JourneyViewModel
            {
                Id = j.Id,
                Name = j.Name,
                Start = j.Start,
                End = j.End,
                RequiredLeaders = j.RequiredLeaders,
                TravelLeaders = leaders
            };
        }).ToList();
    }
}
