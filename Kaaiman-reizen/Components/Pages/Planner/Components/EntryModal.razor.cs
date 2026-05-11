using Kaaiman_reizen.Data.Entities;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner.Components;

public partial class EntryModal : ComponentBase
{
    [Parameter] public bool ActionInProgress { get; set; }
    [Parameter] public List<PlanningVersion> AvailableDrafts { get; set; } = new();

    // Establishing two-way binding properties for the Draft ID
    [Parameter] public int? SelectedDraftId { get; set; }
    [Parameter] public EventCallback<int?> SelectedDraftIdChanged { get; set; }

    [Parameter] public EventCallback OnGenerateNew { get; set; }
    [Parameter] public EventCallback OnResumeDraft { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    protected async Task SelectDraft(int id)
    {
        SelectedDraftId = id;
        await SelectedDraftIdChanged.InvokeAsync(id);
    }
}