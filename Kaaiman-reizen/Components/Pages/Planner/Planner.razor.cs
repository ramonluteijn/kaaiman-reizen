using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Extensions;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class Planner : ComponentBase
{
    [Inject] private ITravelLeaderService LeaderService { get; set; } = default!;
    [Inject] private IJourneyService JourneyService { get; set; } = default!;
    [Inject] private ILogger<Planner> Logger { get; set; } = default!;
    [Inject] private IHostEnvironment HostEnvironment { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<TravelLeaderViewModel> _leaders = [];
    private List<JourneyViewModel> _journeys = [];
    
    private TravelLeaderViewModel? _draggedLeader;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var leaders = await LeaderService.GetTravelLeadersAsync();
            var journeys = await JourneyService.GetJourneysAsync();

            _leaders = leaders.ToViewModels().ToList();
            _journeys = journeys.ToViewModels().ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Kon planner data niet laden");
            _error = $"Kon planner data niet laden: {LoadErrorFormatter.Format(ex, HostEnvironment)}";
            _leaders = [];
            _journeys = [];
        }
        finally
        {
            _loading = false;
        }
    }


/// <summary>
/// Handle the start of a leader drag operation.
/// </summary>
/// <param name="leader">The leader being dragged.</param>
    private void HandleLeaderDragStart(TravelLeaderViewModel leader)
    {
        _draggedLeader = leader;
    }

    private async Task AssignDraggedLeaderToJourney(JourneyViewModel journey)
    {
        if (_draggedLeader == null) return;

        // Check if already assigned
        if (journey.TravelLeaders.Any(l => l.Id == _draggedLeader.Id))
        {
            _draggedLeader = null;
            return;
        }

        try
        {
            // Fetch the existing Journey from DB to update
            var entity = await JourneyService.GetJourneyByIdAsync(journey.Id);
            if (entity != null)
            {
                // Assign new leader
                var currentLeaderIds = entity.TravelLeaders.Select(l => l.Id).ToList();
                if (!currentLeaderIds.Contains(_draggedLeader.Id))
                {
                    currentLeaderIds.Add(_draggedLeader.Id);
                    await JourneyService.UpdateJourneyAsync(entity, currentLeaderIds);

                    // Refresh UI state directly
                    journey.TravelLeaders.Add(_draggedLeader);
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Kan reisleider niet aan reis toewijzen");
        }
        finally
        {
            _draggedLeader = null;
        }
    }
}
