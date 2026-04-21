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

    private int _selectedYear = DateTime.UtcNow.Year;
    private PlannerDraftRequest? _request;
    private PlannerDraftResult?  _result;
    private bool _loading            = true;
    private bool _isGenerating       = false;
    private bool _isSaving           = false;
    private bool _showEntryModal     = true;
    private bool _entryActionInProgress;
    private string?   _saveMessage;
    private Severity  _saveMessageSeverity = Severity.Info;
    private List<PlanningVersion> _availableDrafts = [];
    private int? _selectedDraftId;
    private CancellationTokenSource? _saveMessageCts;
    private Kaaiman_reizen.Data.Rules.CheckRules.RuleSettings _ruleSettings =
        Kaaiman_reizen.Data.Rules.CheckRules.GetDefaultSettings();
    private bool                  _drawerOpen         = false;
    private JourneyViewModel?     _selectedJourney;
    private List<LeaderCandidate> _selectedCandidates = [];
    private bool _sidebarOpen = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataForYearAsync(_selectedYear);
    }

    private async Task LoadDataForYearAsync(int year)
    {
        _loading = true;
        _request = await DraftService.BuildRequestAsync(year);
        var rules = await RuleService.GetRulesAsync();
        _ruleSettings = Kaaiman_reizen.Data.Rules.CheckRules.FromRules(rules);
        var drafts = await PlanningService.GetDraftsAsync(year);
        _availableDrafts = drafts.ToList();
        _selectedDraftId = VersionId is not null && _availableDrafts.Any(d => d.Id == VersionId.Value)
            ? VersionId
            : _availableDrafts.FirstOrDefault()?.Id;
        _loading = false;
    }

    private async Task LoadDraftByIdAsync(int planningVersionId)
    {
        if (_request is null) return;

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
        if (_selectedDraftId is null) return;

        _entryActionInProgress = true;
        try
        {
            await LoadDraftByIdAsync(_selectedDraftId.Value);
            if (_result is not null)
                _showEntryModal = false;
        }
        finally
        {
            _entryActionInProgress = false;
        }
    }

    private async Task GenerateNewPlanningFromModalAsync()
    {
        if (_request is null) return;

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

    private PlannerDraftResult MapToDraftResult(PlanningVersion planningVersion)
    {
        var result = new PlannerDraftResult { IsSuccess = true };

        foreach (var journeyGroup in planningVersion.Assignments.GroupBy(a => a.JourneyId))
        {
            var journey = _request!.Journeys.FirstOrDefault(j => j.Id == journeyGroup.Key);
            if (journey is null)
            {
                result.JourneyWarnings[journeyGroup.Key] = "Deze reis is geannuleerd of verplaatst naar een ander jaar.";
                continue;
            }

            var assignmentsForJourney = new List<JourneyAssignmentResult>();

            foreach (var assignment in journeyGroup)
            {
                var leader = _request.Leaders.FirstOrDefault(l => l.Id == assignment.TravelLeaderId);
                if (leader is null)
                {
                    result.JourneyWarnings[journey.Id] = $"Let op: {assignment.TravelLeader.Name} is automatisch verwijderd omdat deze inactief is of geen beschikbaarheid heeft.";
                    continue;
                }

                if (!leader.AvailabilityPeriods.Any(p => p.Start <= journey.Start && p.End >= journey.End))
                {
                    result.JourneyWarnings[journey.Id] = $"Let op: {leader.Name} is automatisch verwijderd omdat de beschikbaarheid is gewijzigd.";
                    continue;
                }

                int? rank = leader.PreferredDestinations.TryGetValue(journey.Name, out var matchedRank)
                    ? matchedRank : null;

                assignmentsForJourney.Add(new JourneyAssignmentResult
                {
                    LeaderId    = assignment.TravelLeaderId,
                    LeaderName  = leader.Name,
                    RankMatched = rank
                });
            }

            result.JourneyAssignments[journeyGroup.Key] = assignmentsForJourney.OrderBy(a => a.LeaderName).ToList();
        }

        return result;
    }

    private record LeaderPlanningRow(
        string LeaderName,
        List<(PlannerJourneyInput Journey, int? RankMatched)> AssignedJourneys,
        Dictionary<string, int> Preferences
    );

    private List<LeaderPlanningRow> BuildLeaderRows()
    {
        if (_request is null || _result is null) return [];

        return _request.Leaders.Select(leader => new LeaderPlanningRow(
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

    private IReadOnlyList<JourneyViewModel> BuildCalendarJourneys()
    {
        if (_request is null || _result is null) return [];

        return _request.Journeys.Select(j =>
        {
            var leaders = new List<TravelLeaderViewModel>();
            if (_result.JourneyAssignments.TryGetValue(j.Id, out var asgns))
                foreach (var a in asgns)
                    leaders.Add(new TravelLeaderViewModel { Id = a.LeaderId, Name = a.LeaderName });

            return new JourneyViewModel
            {
                Id              = j.Id,
                Name            = j.Name,
                Start           = j.Start,
                End             = j.End,
                RequiredLeaders = j.RequiredLeaders,
                TravelLeaders   = leaders
            };
        }).ToList();
    }
}
