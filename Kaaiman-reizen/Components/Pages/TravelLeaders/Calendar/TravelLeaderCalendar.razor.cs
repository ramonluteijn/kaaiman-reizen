using Kaaiman_reizen.Components.Pages.Planner.Components;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Kaaiman_reizen.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Calendar;

public partial class TravelLeaderCalendar : ComponentBase
{
    private const int SaveMessageDisplayMs = 5000;

    [SupplyParameterFromQuery(Name = "versionId")]
    public int? VersionId { get; set; }

    [Inject] private ITravelLeaderService TravelLeaderService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool _loading = true;
    protected string _statusMessage = string.Empty;
    protected List<Journey> _journeys = [];

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _statusMessage = string.Empty;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is not true)
        {
            _statusMessage = "Log in om je reisgeschiedenis te bekijken.";
            _loading = false;
            return;
        }

        var leaders = await TravelLeaderService.GetTravelLeadersAsync();
        var leader = FindCurrentLeader(leaders, user);

        if (leader is null)
        {
            _statusMessage = "Geen gekoppelde reisleider gevonden voor dit account.";
            _loading = false;
            return;
        }

        _journeys = (leader.Journeys ?? [])
            .OrderByDescending(journey => journey.End)
            .ThenByDescending(journey => journey.Start)
            .ToList();

        _loading = false;
    }
    private static TravelLeader? FindCurrentLeader(IEnumerable<TravelLeader> leaders, ClaimsPrincipal user)
    {
        var leaderIdClaim = user.FindFirstValue("TravelLeaderId");
        if (int.TryParse(leaderIdClaim, out var leaderId))
        {
            var matchedById = leaders.FirstOrDefault(leader => leader.Id == leaderId);
            if (matchedById is not null)
            {
                return matchedById;
            }
        }

        return null;
    }

    private IReadOnlyList<JourneyViewModel> GetCalendarJourneys()
    {
        return _journeys.Select(j =>
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
