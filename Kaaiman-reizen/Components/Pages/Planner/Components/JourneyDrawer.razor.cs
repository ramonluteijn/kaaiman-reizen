using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner.Components;

public partial class JourneyDrawer : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public JourneyViewModel? SelectedJourney { get; set; }
    [Parameter] public List<LeaderCandidate> Candidates { get; set; } = new();

    [Parameter] public EventCallback<TravelLeaderViewModel> OnRemoveLeader { get; set; }
    [Parameter] public EventCallback<LeaderCandidate> OnAssignLeader { get; set; }
}
