using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Kaaiman_reizen.Components.Pages.Archive;

public partial class ArchivedPlanning : ComponentBase
{
    [Inject]
    private IPlanningService PlanningService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    protected bool _loading = true;
    private bool _notFound = false;
    private string? _errorMessage;
    protected List<JourneyViewModel> _journeys = [];

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _notFound = false;
        _journeys = (List<JourneyViewModel>)await BuildCalendarJourneysAsync(Id);
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

    private async Task<IReadOnlyList<JourneyViewModel>> BuildCalendarJourneysAsync(int id)
    {
        var journeys = await PlanningService.GetAllJourneysOfPlanningByIdAsync(id);

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
