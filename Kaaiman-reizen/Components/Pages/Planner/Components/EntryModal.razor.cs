using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Kaaiman_reizen.Components.Pages.Planner.Components;

public partial class EntryModal : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public bool ActionInProgress { get; set; }
    [Parameter] public List<PlanningVersion> AvailableDrafts { get; set; } = new();

    // Establishing two-way binding properties for the Draft ID
    [Parameter] public int? SelectedDraftId { get; set; }
    [Parameter] public EventCallback<int?> SelectedDraftIdChanged { get; set; }

    [Parameter] public EventCallback OnGenerateNew { get; set; }
    [Parameter] public EventCallback OnResumeDraft { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private int _userTimezoneOffsetMinutes;
    private bool _userTimezoneOffsetLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await EnsureUserTimezoneOffsetAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task EnsureUserTimezoneOffsetAsync()
    {
        if (_userTimezoneOffsetLoaded) return;

        try
        {
            _userTimezoneOffsetMinutes = await JS.InvokeAsync<int>("kaaimanDateTime.getTimezoneOffsetMinutes");
        }
        catch
        {
            _userTimezoneOffsetMinutes = 0;
        }
        finally
        {
            _userTimezoneOffsetLoaded = true;
        }
    }

    protected string FormatCreatedAt(DateTime createdAt)
    {
        var userLocal = DateDisplay.ToUserLocal(createdAt, _userTimezoneOffsetMinutes);
        return DateDisplay.FormatDateTime(userLocal);
    }

    protected async Task SelectDraft(int id)
    {
        SelectedDraftId = id;
        await SelectedDraftIdChanged.InvokeAsync(id);
    }
}