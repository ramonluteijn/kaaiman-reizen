using Kaaiman_reizen.Models.Planner;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner.Components;

public partial class NoteModal
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public PlannerLeaderInput? Leader { get; set; }
    [Parameter] public List<JourneyDetail> AssignedJourneys { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }

    private string DisplayNote =>
        string.IsNullOrWhiteSpace(Leader?.Note) ? "Geen notitie beschikbaar." : Leader.Note;

    private void EditPreferences()
    {
        if (Leader is null) return;
        Navigation.NavigateTo($"/travelleaders/preferences/{Leader.Id}");
    }

    public sealed record JourneyDetail(string Name, DateOnly Start, DateOnly End, int? RankMatched);
}