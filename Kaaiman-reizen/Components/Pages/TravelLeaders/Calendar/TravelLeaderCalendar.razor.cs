using System.Security.Claims;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Calendar;

public partial class TravelLeaderCalendar : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool _loading = true;
    protected string _statusMessage = string.Empty;
    protected List<JourneyViewModel> _journeys = [];

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _statusMessage = string.Empty;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is not true)
        {
            _statusMessage = "Log in om je kalender te bekijken.";
            _loading = false;
            return;
        }

        var leaders = await LeaderService.GetTravelLeadersAsync();
        var leader = FindCurrentLeader(leaders, user);

        if (leader is null)
        {
            _statusMessage = "Geen gekoppelde reisleider gevonden voor dit account.";
            _loading = false;
            return;
        }

        _journeys = (List<JourneyViewModel>)await BuildCalendarJourneysAsync(leader);

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

    private async Task<IReadOnlyList<JourneyViewModel>> BuildCalendarJourneysAsync(TravelLeader leader)
    {
        var journeys = await LeaderService.GetJourneysOfTravelLeaderAsync(leader);
        Console.WriteLine($"Travel leader journeys count: {journeys.Count}");
        return journeys.Select(j =>
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
