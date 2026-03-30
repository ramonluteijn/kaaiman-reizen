using Kaaiman_reizen.Data.Rules;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Extensions;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;

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
    
    private JourneyViewModel? _selectedJourney;
    private bool _isDrawerOpen = false;

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

    private void SelectJourney(JourneyViewModel journey)
    {
        Console.WriteLine($"Selected journey: {journey.Id}");
        _selectedJourney = journey;
        _isDrawerOpen = true;
        Console.WriteLine($"Drawer open: {_isDrawerOpen}");
        InvokeAsync(StateHasChanged);
    }

    private void CloseDrawer()
    {
        _isDrawerOpen = false;
    }

    private List<TravelLeaderViewModel> GetAvailableLeaders()
    {
        if (_selectedJourney == null) return [];

        var assignedLeaderIds = _selectedJourney.TravelLeaders.Select(l => l.Id).ToList();
        return GetPossibleLeaders(assignedLeaderIds);
    }

    private List<TravelLeaderViewModel> GetPossibleLeaders(List<int> assignedLeaderIds)
    {
        if (_selectedJourney == null) return [];

        var selectedJourney = _selectedJourney;

        var candidates = _leaders
            .Where(l => l.IsActive && !assignedLeaderIds.Contains(l.Id))
            .Select(leader =>
            {
                var leaderJourneys = _journeys
                    .Where(j => j.Id != selectedJourney.Id && j.TravelLeaders.Any(tl => tl.Id == leader.Id))
                    .ToList();

                var evaluation = CheckRules.EvaluateForPlanner(
                    leaderJourneys.Select(j => new CheckRules.JourneyWindow(j.Start, j.End)),
                    selectedJourney.Start,
                    selectedJourney.End,
                    leader.MinTrips,
                    leader.MaxTrips);

                return new LeaderCandidateDto(
                    Leader: leader,
                    NoOverlap: evaluation.NoOverlap,
                    HasThreeDayBuffer: evaluation.HasMinimumGap,
                    CurrentTripCount: leaderJourneys.Count,
                    MinMaxResult: evaluation.MinMaxResult
                );
            })
            // Only keep eligible candidates
            .Where(c => c.NoOverlap && c.HasThreeDayBuffer && c.MinMaxResult.IsWithinLimitsAfterAssignment && !c.MinMaxResult.ExceedsMaxAfterAssignment)
            .OrderByDescending(c => c.MinMaxResult.DistanceFromMinMax) // Furthest from min/max first
            .ThenByDescending(c => c.MinMaxResult.BelowMinAfterAssignment) // Prefer those below min
            .ThenBy(c => c.CurrentTripCount) // Fewer trips first
            .ThenBy(c => c.Leader.Name)
            .Select(c => c.Leader)
            .ToList();

        return candidates;
    }
    
    private bool IsLeaderEligibleForJourney(int leaderId, int journeyId, DateTime journeyStart, DateTime journeyEnd, out string? reason)
    {
        var leader = _leaders.FirstOrDefault(l => l.Id == leaderId);
        if (leader == null)
        {
            reason = "Deze reisleider kon niet worden gevonden.";
            return false;
        }

        var leaderJourneys = _journeys
            .Where(j => j.Id != journeyId && j.TravelLeaders.Any(tl => tl.Id == leaderId))
            .ToList();

        return CheckRules.CanAssignForPlanner(
            leaderJourneys.Select(j => new CheckRules.JourneyWindow(j.Start, j.End)),
            journeyStart,
            journeyEnd,
            leader.MinTrips,
            leader.MaxTrips,
            out reason);
    }

    private async Task AssignLeader(TravelLeaderViewModel leader)
    {
        if (_selectedJourney == null) return;
        
        if (_selectedJourney.TravelLeaders?.Any(l => l.Id == leader.Id) == true) return;

        if (!IsLeaderEligibleForJourney(leader.Id, _selectedJourney.Id, _selectedJourney.Start, _selectedJourney.End, out var blockReason))
        {
            _error = blockReason;
            return;
        }

        try
        {
            var entity = await JourneyService.GetJourneyByIdAsync(_selectedJourney.Id);
            if (entity != null)
            {
                var currentLeaderIds = entity.TravelLeaders?.Select(l => l.Id).ToList() ?? new List<int>();
                if (!currentLeaderIds.Contains(leader.Id))
                {
                    currentLeaderIds.Add(leader.Id);
                    await JourneyService.UpdateJourneyAsync(entity, currentLeaderIds);

                    _selectedJourney.TravelLeaders?.Add(leader);
                    _error = null;
                    _isDrawerOpen = false; 
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Kan reisleider niet aan reis toewijzen");
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
        if (journey.TravelLeaders?.Any(l => l.Id == _draggedLeader.Id) == true)
        {
            _draggedLeader = null;
            return;
        }

        if (!IsLeaderEligibleForJourney(_draggedLeader.Id, journey.Id, journey.Start, journey.End, out var blockReason))
        {
            _error = blockReason;
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
                var currentLeaderIds = entity.TravelLeaders?.Select(l => l.Id).ToList() ?? new List<int>();
                if (!currentLeaderIds.Contains(_draggedLeader.Id))
                {
                    currentLeaderIds.Add(_draggedLeader.Id);
                    await JourneyService.UpdateJourneyAsync(entity, currentLeaderIds);

                    // Refresh UI state directly
                    journey.TravelLeaders?.Add(_draggedLeader);
                    _error = null;
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
