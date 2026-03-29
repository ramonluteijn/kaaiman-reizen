using Kaaiman_reizen.Components.Shared;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Extensions;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using MudBlazor;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders;

public partial class TravelLeaders
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ILogger<TravelLeaders> Logger { get; set; } = default!;

    [Inject]
    private IHostEnvironment HostEnvironment { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<TravelLeaderViewModel> _leaders = [];

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var items = await LeaderService.GetTravelLeadersAsync();
            _leaders = items.ToViewModels().ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Kon reisleiders niet laden");
            _error = $"Kon reisleiders niet laden: {LoadErrorFormatter.Format(ex, HostEnvironment)}";
            _leaders = [];
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnDelete(TravelLeaderViewModel leader)
    {
        var parameters = new DialogParameters<DeleteTravelLeaderDialog>
        {
            { x => x.LeaderName, leader.Name }
        };

        var dialog = await DialogService.ShowAsync<DeleteTravelLeaderDialog>(
            "Reisleider verwijderen",
            parameters);

        var result = await dialog.Result;
        if (result.Canceled)
            return;

        await LeaderService.DeleteTravelLeaderAsync(leader.Id);
        _leaders.RemoveAll(l => l.Id == leader.Id);
    }

    private static string GetEditHref(TravelLeaderViewModel leader)
    {
        return $"/travelleaders/edit/{leader.Id}";
    }

    private async Task ToggleActive(TravelLeaderViewModel? leader)
    {
        if (leader == null)
            return;

        var entity = await LeaderService.GetTravelLeaderByIdAsync(leader.Id);
        if (entity != null)
        {
            entity.IsActive = !entity.IsActive;
            await LeaderService.UpdateTravelLeaderAsync(entity);

            var idx = _leaders.FindIndex(l => l.Id == leader.Id);
            if (idx >= 0)
            {
                _leaders[idx] = entity.ToViewModel();
                StateHasChanged();
            }
        }
    }
}