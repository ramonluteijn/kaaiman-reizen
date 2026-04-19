using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Kaaiman_reizen.Services;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerDraft : ComponentBase
{
    private const int SaveMessageDisplayMs = 5000;

    [SupplyParameterFromQuery(Name = "versionId")]
    public int? VersionId { get; set; }

    [Inject] private IPlannerDraftService DraftService { get; set; } = default!;
    [Inject] private IPlanningService PlanningService { get; set; } = default!;
    [Inject] private IRuleService RuleService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private PlannerDraftRequest? _request;
    private PlannerDraftResult? _result;
    private bool _loading = true;
    private bool _isGenerating = false;
    private bool _isSaving = false;
    private bool _showEntryModal = true;
    private bool _entryActionInProgress;
    private string? _saveMessage;
    private Severity _saveMessageSeverity = Severity.Info;
    private List<PlanningVersion> _availableDrafts = [];
    private int? _selectedDraftId;
    private CancellationTokenSource? _saveMessageCts;
    private List<(string Country, int Count)> _topPopular = [];
    private List<(string Country, int Count)> _leastPopular = [];
    private List<PlannerLeaderInput> _multiInterestLeaders = [];
    private Kaaiman_reizen.Data.Rules.CheckRules.RuleSettings _ruleSettings = Kaaiman_reizen.Data.Rules.CheckRules.GetDefaultSettings();

    // ── Drawer state ───────────────────────────────────────────
    private bool _drawerOpen = false;
    private JourneyViewModel? _selectedJourney;

    // ── Initialisation ─────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        _request = await DraftService.BuildRequestAsync();
        var rules = await RuleService.GetRulesAsync();
        _ruleSettings = Kaaiman_reizen.Data.Rules.CheckRules.FromRules(rules);
        ComputeInsights();
        var drafts = await PlanningService.GetDraftsAsync();
        _availableDrafts = drafts.ToList();
        _selectedDraftId = VersionId is not null && _availableDrafts.Any(draft => draft.Id == VersionId.Value)
            ? VersionId
            : _availableDrafts.FirstOrDefault()?.Id;
        _loading = false;
    }

    private void ComputeInsights()
    {
        if (_request is null) return;

        var allCountries = _request.Journeys.Select(j => j.Name).Distinct();

        var popularity = allCountries
            .Select(c => (
                Country: c,
                Count: _request.Leaders.Count(l => l.PreferredDestinations.ContainsKey(c))
            ))
            .ToList();

        _topPopular = popularity.OrderByDescending(p => p.Count).Take(3).ToList();
        _leastPopular = popularity.OrderBy(p => p.Count).Take(3).ToList();
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
        _result = null;
        ClearSaveMessage();
        StateHasChanged();
        await Task.Delay(50);
        _result = await Task.Run(() => DraftService.GenerateDraft(_request));
        _isGenerating = false;
    }

    private async Task LoadDraftByIdAsync(int planningVersionId)
    {
        if (_request is null)
        {
            return;
        }

        var selectedDraft = await PlanningService.GetPlanningVersionByIdAsync(planningVersionId);
        if (selectedDraft is null || selectedDraft.IsPublished)
        {
            SetSaveMessage($"Concept met id {planningVersionId} is niet gevonden.", Severity.Warning);
            Snackbar.Add(_saveMessage, _saveMessageSeverity);
            return;
        }

        _result = MapToDraftResult(selectedDraft);
    }

    private async Task ResumeSelectedDraftAsync()
    {
        if (_selectedDraftId is null)
        {
            return;
        }

        _entryActionInProgress = true;

        try
        {
            await LoadDraftByIdAsync(_selectedDraftId.Value);
            if (_result is not null)
            {
                _showEntryModal = false;
            }
        }
        finally
        {
            _entryActionInProgress = false;
        }
    }

    private async Task GenerateNewPlanningFromModalAsync()
    {
        if (_request is null)
        {
            return;
        }

        _entryActionInProgress = true;
        _isGenerating = true;
        _result = null;
        ClearSaveMessage();
        StateHasChanged();

        try
        {
            await Task.Delay(50);
            _result = await Task.Run(() => DraftService.GenerateDraft(_request));
            _showEntryModal = false;
        }
        finally
        {
            _isGenerating = false;
            _entryActionInProgress = false;
        }
    }

    private async Task SaveDraftAsync()
    {
        await SavePlanningAsync(isPublished: false);
    }

    private async Task PublishPlanningAsync()
    {
        await SavePlanningAsync(isPublished: true);
    }

    private async Task SavePlanningAsync(bool isPublished)
    {
        if (_request is null || _result is null || !_result.IsSuccess)
        {
            return;
        }

        _isSaving = true;
        ClearSaveMessage();

        try
        {
            var assignments = _result.JourneyAssignments.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyCollection<int>)pair.Value.Select(assignment => assignment.LeaderId).ToList());

            string planningName = isPublished
                ? $"Published planning {DateTime.Now:yyyy-MM-dd HH:mm}"
                : $"Draft planning {DateTime.Now:yyyy-MM-dd HH:mm}";

            await PlanningService.SavePlanningAsync(planningName, isPublished, assignments);

            var message = isPublished
                ? "Planning is gepubliceerd. Andere gebruikers zien nu deze versie."
                : "Conceptplanning is opgeslagen.";
            var severity = isPublished ? Severity.Success : Severity.Info;

            SetSaveMessage(message, severity);
            Snackbar.Add(message, severity);
        }
        catch (Exception)
        {
            var message = isPublished
                ? "Publiceren is mislukt."
                : "Opslaan van het concept is mislukt.";
            var severity = Severity.Error;

            SetSaveMessage(message, severity);
            Snackbar.Add(message, severity);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void SetSaveMessage(string message, Severity severity)
    {
        _saveMessage = message;
        _saveMessageSeverity = severity;
        StartSaveMessageAutoHide();
    }

    private void ClearSaveMessage()
    {
        _saveMessageCts?.Cancel();
        _saveMessageCts?.Dispose();
        _saveMessageCts = null;
        _saveMessage = null;
    }

    private void StartSaveMessageAutoHide()
    {
        _saveMessageCts?.Cancel();
        _saveMessageCts?.Dispose();
        _saveMessageCts = new CancellationTokenSource();
        var token = _saveMessageCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SaveMessageDisplayMs, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await InvokeAsync(() =>
                {
                    _saveMessage = null;
                    StateHasChanged();
                });
            }
            catch (TaskCanceledException)
            {
                // Intentionally ignored because newer message replaced this one.
            }
        }, token);
    }

    private PlannerDraftResult MapToDraftResult(Kaaiman_reizen.Data.Entities.PlanningVersion planningVersion)
    {
        var result = new PlannerDraftResult
        {
            IsSuccess = true
        };

        foreach (var journeyGroup in planningVersion.Assignments.GroupBy(assignment => assignment.JourneyId))
        {
            var journey = _request!.Journeys.FirstOrDefault(item => item.Id == journeyGroup.Key);
            if (journey is null)
            {
                continue;
            }

            result.JourneyAssignments[journeyGroup.Key] = journeyGroup
                .Select(assignment =>
                {
                    var leader = _request.Leaders.FirstOrDefault(item => item.Id == assignment.TravelLeaderId);
                    int? rank = leader is not null &&
                                leader.PreferredDestinations.TryGetValue(journey.Name, out var matchedRank)
                        ? matchedRank
                        : null;

                    return new JourneyAssignmentResult
                    {
                        LeaderId = assignment.TravelLeaderId,
                        LeaderName = assignment.TravelLeader.Name,
                        RankMatched = rank
                    };
                })
                .OrderBy(item => item.LeaderName)
                .ToList();
        }

        return result;
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
                    Journey: _request.Journeys.First(j => j.Id == kvp.Key),
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
                Id = j.Id,
                Name = j.Name,
                Start = j.Start,
                End = j.End,
                RequiredLeaders = j.RequiredLeaders,
                TravelLeaders = leaders
            };
        }).ToList();
    }

    // ── Drawer handlers ────────────────────────────────────────

    private void HandleJourneyClick(JourneyViewModel journey)
    {
        _selectedJourney = journey;
        _drawerOpen = true;
    }

    private void CloseDrawer()
    {
        _drawerOpen = false;
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
            Id = _selectedJourney.Id,
            Name = _selectedJourney.Name,
            Start = _selectedJourney.Start,
            End = _selectedJourney.End,
            RequiredLeaders = journeyInput.RequiredLeaders,
            TravelLeaders = leaders
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
        int? rank = leader.PreferredDestinations.TryGetValue(_selectedJourney.Name, out int r) ? r : null;

        var entry = new JourneyAssignmentResult
        {
            LeaderId = candidate.LeaderId,
            LeaderName = candidate.LeaderName,
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
        int LeaderId,
        string LeaderName,
        int? PreferenceRank,
        bool IsAlreadyAssigned,
        bool HasConflict,
        string ConflictJourneyName,
        bool ExceedsMaxTrips,
        int CurrentAssignments,
        int MaxTrips
        string? ValidationReason
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

        // Show all leaders; if they cannot be assigned, surface the first blocking reason.
        return _request.Leaders
            .Select(leader =>
            {
                bool isAvailableForJourney = leader.AvailabilityPeriods.Any(
                    p => p.Start <= journeyInput.Start && p.End >= journeyInput.End);

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

                int? rank = leader.PreferredDestinations.TryGetValue(journey.Name, out int r) ? r : null;

                // Use CheckRules.CanAssignForPlanner to get the reason
                string? validationReason;
                var journeyEntity = new Journey {
                    Id = journeyInput.Id,
                    Name = journeyInput.Name,
                    Start = journeyInput.Start,
                    End = journeyInput.End
                };
                var leaderEntity = new TravelLeader {
                    Id = leader.Id,
                    Name = leader.Name,
                    AmountOfTrips = leader.AmountOfTrips,
                    MinTrips = leader.MinTrips,
                    MaxTrips = leader.MaxTrips
                };
                var existingJourneys = _result.JourneyAssignments
                    .Where(kvp => kvp.Value.Any(a => a.LeaderId == leader.Id) && kvp.Key != journey.Id)
                    .Select(kvp => {
                        var j = _request.Journeys.FirstOrDefault(x => x.Id == kvp.Key);
                        return j is not null ? new Kaaiman_reizen.Data.Rules.CheckRules.JourneyWindow(j.Start, j.End) : null;
                    })
                    .Where(x => x != null)
                    .Cast<Kaaiman_reizen.Data.Rules.CheckRules.JourneyWindow>();

                Kaaiman_reizen.Data.Rules.CheckRules.CanAssignForPlanner(
                    existingJourneys,
                    journeyEntity,
                    leaderEntity,
                    out validationReason,
                    _ruleSettings
                );

                if (!isAvailableForJourney)
                {
                    validationReason = "Deze reisleider is niet beschikbaar voor deze reisdatums.";
                }

                return new LeaderCandidate(
                    LeaderId: leader.Id,
                    LeaderName: leader.Name,
                    PreferenceRank: rank,
                    IsAlreadyAssigned: assignedToThis.Contains(leader.Id),
                    HasConflict: conflictJourney is not null,
                    ConflictJourneyName: conflictJourney is not null
                        ? $"{conflictJourney.Name} ({conflictJourney.Start:dd MMM}–{conflictJourney.End:dd MMM})"
                        : string.Empty,
                    ExceedsMaxTrips: currentCount >= leader.MaxTrips,
                    CurrentAssignments: currentCount,
                    MaxTrips: leader.MaxTrips
                    ValidationReason: validationReason
                );
            })
            // Sort: already-assigned first, then clean candidates, then flagged
            .OrderBy(c => c.IsAlreadyAssigned ? 0 : 1)
            .ThenBy(c => (c.HasConflict || c.ExceedsMaxTrips || !string.IsNullOrEmpty(c.ValidationReason)) ? 1 : 0)
            .ThenBy(c => c.PreferenceRank ?? 99)
            .ToList();
    }
}
