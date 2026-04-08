using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Kaaiman_reizen.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerDraft : ComponentBase
{
    [Inject] private IPlannerDraftService DraftService { get; set; } = default!;

    private PlannerDraftRequest? _request;
    private PlannerDraftResult?  _result;
    private bool _loading     = true;
    private bool _isGenerating = false;
    private List<(string Country, int Count)> _topPopular       = [];
    private List<(string Country, int Count)> _leastPopular     = [];
    private List<PlannerLeaderInput>          _multiInterestLeaders = [];

    // ── Drawer state ───────────────────────────────────────────
    private bool             _drawerOpen      = false;
    private JourneyViewModel? _selectedJourney;

    // ── Initialisation ─────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        _request = await DraftService.BuildRequestAsync();
        ComputeInsights();
        _loading = false;
    }

    private void ComputeInsights()
    {
        if (_request is null) return;

        var allCountries = _request.Journeys.Select(j => j.Country).Distinct();

        var popularity = allCountries
            .Select(c => (
                Country: c,
                Count: _request.Leaders.Count(l => l.PreferredDestinations.ContainsKey(c))
            ))
            .ToList();

        _topPopular           = popularity.OrderByDescending(p => p.Count).Take(3).ToList();
        _leastPopular         = popularity.OrderBy(p => p.Count).Take(3).ToList();
        _multiInterestLeaders = _request.Leaders
            .Where(l => l.PreferredDestinations.Count > 1)
            .OrderByDescending(l => l.PreferredDestinations.Count)
            .ToList();
    }

    // ── Generation ─────────────────────────────────────────────

    private async Task GenerateDraftAsync()
    {
        if (_request is null) return;
        _isGenerating = true;
        _result       = null;
        StateHasChanged();
        await Task.Delay(50);
        _result       = await Task.Run(() => DraftService.GenerateDraft(_request));
        _isGenerating = false;
    }

    // ── Leader overview rows ────────────────────────────────────

    private record LeaderPlanningRow(
        int LeaderId,
        string LeaderName,
        List<(PlannerJourneyInput Journey, int? RankMatched)> AssignedJourneys,
        Dictionary<string, int> Preferences
    );

    private List<LeaderPlanningRow> BuildLeaderRows()
    {
        if (_request is null || _result is null || !_result.IsSuccess) return [];

        return _request.Leaders.Select(leader => new LeaderPlanningRow(
            leader.Id,
            leader.Name,
            _result.JourneyAssignments
                .Where(kvp => kvp.Value.Any(a => a.LeaderId == leader.Id))
                .Select(kvp => (
                    Journey:     _request.Journeys.First(j => j.Id == kvp.Key),
                    RankMatched: kvp.Value.First(a => a.LeaderId == leader.Id).RankMatched
                ))
                .ToList(),
            leader.PreferredDestinations
        )).ToList();
    }

    // ── Calendar journey list ───────────────────────────────────

    private IReadOnlyList<JourneyViewModel> BuildCalendarJourneys()
    {
        if (_request is null || _result is null || !_result.IsSuccess) return [];

        return _request.Journeys.Select(j =>
        {
            var leaders = new List<TravelLeaderViewModel>();
            if (_result.JourneyAssignments.TryGetValue(j.Id, out var asgns))
                foreach (var a in asgns)
                    leaders.Add(new TravelLeaderViewModel { Id = a.LeaderId, Name = a.LeaderName });

            return new JourneyViewModel
            {
                Id              = j.Id,
                Country         = j.Country,
                Start           = j.Start,
                End             = j.End,
                RequiredLeaders = j.RequiredLeaders,
                TravelLeaders   = leaders
            };
        }).ToList();
    }

    // ── Drawer handlers ────────────────────────────────────────

    private void HandleJourneyClick(JourneyViewModel journey)
    {
        _selectedJourney = journey;
        _drawerOpen      = true;
    }

    private void CloseDrawer()
    {
        _drawerOpen      = false;
        _selectedJourney = null;
    }

    /// <summary>Rebuilds _selectedJourney from _result so the drawer immediately reflects changes.</summary>
    private void RefreshSelectedJourney()
    {
        if (_selectedJourney is null || _request is null || _result is null) return;

        var journeyInput = _request.Journeys.First(j => j.Id == _selectedJourney.Id);
        var leaders = _result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var asgns)
            ? asgns.Select(a => new TravelLeaderViewModel { Id = a.LeaderId, Name = a.LeaderName }).ToList()
            : new List<TravelLeaderViewModel>();

        _selectedJourney = new JourneyViewModel
        {
            Id              = _selectedJourney.Id,
            Country         = _selectedJourney.Country,
            Start           = _selectedJourney.Start,
            End             = _selectedJourney.End,
            RequiredLeaders = journeyInput.RequiredLeaders,
            TravelLeaders   = leaders
        };
    }

    private void RemoveLeader(TravelLeaderViewModel leader)
    {
        if (_selectedJourney is null || _result is null) return;

        if (_result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var list))
        {
            list.RemoveAll(a => a.LeaderId == leader.Id);
            if (list.Count == 0)
                _result.JourneyAssignments.Remove(_selectedJourney.Id);
        }
        RefreshSelectedJourney();
    }

    private void AssignLeader(LeaderCandidate candidate)
    {
        if (_selectedJourney is null || _result is null || _request is null) return;

        // Guard: don't add the same leader twice
        if (_result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var current)
            && current.Any(a => a.LeaderId == candidate.LeaderId))
            return;

        // Guard: respect the RequiredLeaders cap — don't assign beyond the limit
        var journeyInput = _request.Journeys.First(j => j.Id == _selectedJourney.Id);
        int currentCount = _result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var existing)
            ? existing.Count : 0;
        if (currentCount >= journeyInput.RequiredLeaders) return;

        var leader = _request.Leaders.First(l => l.Id == candidate.LeaderId);
        int? rank  = leader.PreferredDestinations.TryGetValue(_selectedJourney.Country, out int r) ? r : null;

        var entry = new JourneyAssignmentResult
        {
            LeaderId    = candidate.LeaderId,
            LeaderName  = candidate.LeaderName,
            RankMatched = rank
        };

        if (_result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var list))
            list.Add(entry);
        else
            _result.JourneyAssignments[_selectedJourney.Id] = [entry];

        RefreshSelectedJourney();
    }

    // ── Candidate helpers ──────────────────────────────────────

    private record LeaderCandidate(
        int    LeaderId,
        string LeaderName,
        int?   PreferenceRank,
        bool   IsAlreadyAssigned,
        bool   HasConflict,
        string ConflictJourneyName,
        bool   ExceedsMaxTrips,
        int    CurrentAssignments,
        int    MaxTrips
    );

    /// <summary>
    /// Returns every leader who has availability for the journey dates.
    /// Leaders with conflicts or MaxTrips exceeded are included but flagged.
    /// </summary>
    private List<LeaderCandidate> GetCandidatesFor(JourneyViewModel journey)
    {
        if (_request is null || _result is null) return [];

        var journeyInput = _request.Journeys.FirstOrDefault(j => j.Id == journey.Id);
        if (journeyInput is null) return [];

        var assignedToThis = _result.JourneyAssignments.TryGetValue(journey.Id, out var cur)
            ? cur.Select(a => a.LeaderId).ToHashSet()
            : new HashSet<int>();

        return _request.Leaders
            .Where(leader => leader.AvailabilityPeriods.Any(
                p => p.Start <= journeyInput.Start && p.End >= journeyInput.End))
            .Select(leader =>
            {
                // Count how many journeys this leader is currently assigned to
                int currentCount = _result.JourneyAssignments.Values
                    .SelectMany(l => l)
                    .Count(a => a.LeaderId == leader.Id);

                // Find an overlapping journey they are already assigned to (other than this one)
                var conflictJourney = _result.JourneyAssignments
                    .Where(kvp => kvp.Key != journey.Id && kvp.Value.Any(a => a.LeaderId == leader.Id))
                    .Select(kvp => _request.Journeys.FirstOrDefault(j => j.Id == kvp.Key))
                    .Where(j => j is not null && j.Start < journeyInput.End && journeyInput.Start < j.End)
                    .FirstOrDefault();

                int? rank = leader.PreferredDestinations.TryGetValue(journey.Country, out int r) ? r : null;

                return new LeaderCandidate(
                    LeaderId           : leader.Id,
                    LeaderName         : leader.Name,
                    PreferenceRank     : rank,
                    IsAlreadyAssigned  : assignedToThis.Contains(leader.Id),
                    HasConflict        : conflictJourney is not null,
                    ConflictJourneyName: conflictJourney is not null
                        ? $"{conflictJourney.Country} ({conflictJourney.Start:dd MMM}–{conflictJourney.End:dd MMM})"
                        : string.Empty,
                    ExceedsMaxTrips    : currentCount >= leader.MaxTrips,
                    CurrentAssignments : currentCount,
                    MaxTrips           : leader.MaxTrips
                );
            })
            // Sort: already-assigned first, then clean candidates, then flagged
            .OrderBy(c => c.IsAlreadyAssigned ? 0 : 1)
            .ThenBy(c  => (c.HasConflict || c.ExceedsMaxTrips) ? 1 : 0)
            .ThenBy(c  => c.PreferenceRank ?? 99)
            .ToList();
    }
}
