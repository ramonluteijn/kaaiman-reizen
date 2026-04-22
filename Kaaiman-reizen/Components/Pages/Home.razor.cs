using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using static Kaaiman_reizen.Data.Services.TravelLeaderService;

namespace Kaaiman_reizen.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IPlanningService PlanningService { get; set; } = default!;
    [Inject] private ITravelLeaderService TravelLeaderService { get; set; } = default!;
    [Inject] private IJourneyService JourneyService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private bool _loading = true;
    private bool _isPlanner;
    private bool _isReisleider;
    private List<PlanningVersion> _drafts = [];
    private List<Journey> _publishedJourneys = [];
    private int _selectedYear = DateTime.UtcNow.Year;

    private List<TravelLeader> _travelLeadersWithoutPreferences = [];
    private List<TravelLeader> _travelLeadersWithoutJourneys = [];
    private List<TravelLeader> _travelLeadersWithNotes = [];
    private List<Journey> _journeysWithoutTravelLeaders = [];
    private List<OverlapData> _travelLeadersWithOverlappingJourneys = [];

    protected override async Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;

        _isPlanner = user.IsInRole("Planner");
        _isReisleider = user.IsInRole("Reisleider");

        if (_isPlanner)
        {
            var drafts = await PlanningService.GetDraftsAsync(_selectedYear);
            _drafts = drafts.ToList();

            _travelLeadersWithoutPreferences = await TravelLeaderService.GetTravelLeadersWithoutPreferencesAsync();
            _travelLeadersWithoutJourneys = await TravelLeaderService.GetTravelLeadersWithoutJourneysAsync(_selectedYear);
            _travelLeadersWithNotes = await TravelLeaderService.GetTravelLeadersWithNotesAsync();
            _journeysWithoutTravelLeaders = await TravelLeaderService.GetJourneysWithoutTravelLeadersAsync(_selectedYear);
            _travelLeadersWithOverlappingJourneys = await TravelLeaderService.GetTravelLeadersWithOverlappingJourneys();
        }


        if (_isReisleider)
        {
            var journeys = await JourneyService.GetJourneysWithPublishedPlanningAsync(_selectedYear);
            _publishedJourneys = journeys.ToList();
        }

        _loading = false;
    }

    private static string FormatTravelLeaders(Journey journey)
    {
        var leaders = journey.TravelLeaders ?? [];
        if (leaders.Count == 0)
        {
            return "-";
        }

        return string.Join("; ", leaders.OrderBy(leader => leader.Name).Select(leader => leader.Name));
    }
}
