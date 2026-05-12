using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Kaaiman_reizen.Services;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Components.Pages.Planner.Components;
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
    private PlannerDraftResult? _result;
    private bool _loading = true;
    private bool _isGenerating = false;
    private bool _isSaving = false;
    private string? _saveMessage;
    private Severity _saveMessageSeverity = Severity.Info;
    private CancellationTokenSource? _saveMessageCts;
    private Kaaiman_reizen.Data.Rules.CheckRules.RuleSettings _ruleSettings =
        Kaaiman_reizen.Data.Rules.CheckRules.GetDefaultSettings();
    private bool _drawerOpen = false;
    private JourneyViewModel? _selectedJourney;
    private List<LeaderCandidate> _selectedCandidates = [];
    private bool _sidebarOpen = true;
    private bool _noteModalOpen;
    private LeaderPlanningRow? _selectedLeaderRow;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataForYearAsync(_selectedYear);
    }

    private async Task LoadDataForYearAsync(int year)
    {
        _loading = true;
        _result = null;
        _request = await DraftService.BuildRequestAsync(year);
        var rules = await RuleService.GetRulesAsync();
        _ruleSettings = Kaaiman_reizen.Data.Rules.CheckRules.FromRules(rules);
        var drafts = await PlanningService.GetDraftsAsync(year);
        var draftToLoad = VersionId is not null
            ? drafts.FirstOrDefault(d => d.Id == VersionId.Value)
            : drafts.FirstOrDefault();
        _loading = false;
        StateHasChanged();

        if (draftToLoad is not null)
        {
            bool isStale = await LoadDraftByIdAsync(draftToLoad.Id);
            if (isStale)
                await RegeneratePlanningAsync(autoUpdated: true);
        }
        else
        {
            await RegeneratePlanningAsync(autoUpdated: false);
        }
    }

    private async Task<bool> LoadDraftByIdAsync(int planningVersionId)
    {
        if (_request is null) return false;

        var selectedDraft = await PlanningService.GetPlanningVersionByIdAsync(planningVersionId);
        if (selectedDraft is null || selectedDraft.IsPublished)
        {
            SetSaveMessage($"Concept met id {planningVersionId} is niet gevonden.", Severity.Warning);
            Snackbar.Add(_saveMessage, _saveMessageSeverity);
            return false;
        }

        var (mapped, isStale) = MapToDraftResult(selectedDraft);
        _result = mapped;
        return isStale;
    }

    private async Task RegeneratePlanningAsync(bool autoUpdated = false)
    {
        if (_request is null) return;

        _isGenerating = true;
        _result = null;
        ClearSaveMessage();
        StateHasChanged();

        await Task.Delay(50);
        _result = await Task.Run(() => DraftService.GenerateDraft(_request));
        _isGenerating = false;

        if (autoUpdated)
            SetSaveMessage("Planning automatisch bijgewerkt omdat reisleider- of reisgegevens zijn gewijzigd.", Severity.Info);
    }

    private (PlannerDraftResult Result, bool IsStale) MapToDraftResult(PlanningVersion planningVersion)
    {
        var result = new PlannerDraftResult { IsSuccess = true };
        bool isStale = false;

        foreach (var journeyGroup in planningVersion.Assignments.GroupBy(a => a.JourneyId))
        {
            var journey = _request!.Journeys.FirstOrDefault(j => j.Id == journeyGroup.Key);
            if (journey is null)
            {
                result.JourneyWarnings[journeyGroup.Key] = "Deze reis is geannuleerd of verplaatst naar een ander jaar.";
                isStale = true;
                continue;
            }

            var assignmentsForJourney = new List<JourneyAssignmentResult>();

            foreach (var assignment in journeyGroup)
            {
                var leader = _request.Leaders.FirstOrDefault(l => l.Id == assignment.TravelLeaderId);
                if (leader is null)
                {
                    result.JourneyWarnings[journey.Id] = $"Let op: {assignment.TravelLeader.Name} is automatisch verwijderd omdat deze inactief is of geen voorkeuren heeft.";
                    isStale = true;
                    continue;
                }

                if (!leader.PreferredDestinations.ContainsKey(journey.Id))
                {
                    result.JourneyWarnings[journey.Id] = $"Let op: {leader.Name} is automatisch verwijderd omdat de voorkeuren zijn gewijzigd.";
                    isStale = true;
                    continue;
                }

                int? rank = leader.PreferredDestinations.TryGetValue(journey.Id, out var matchedRank)
                    ? (matchedRank == 0 ? null : matchedRank) : null;

                assignmentsForJourney.Add(new JourneyAssignmentResult
                {
                    LeaderId = assignment.TravelLeaderId,
                    LeaderName = leader.Name,
                    RankMatched = rank
                });
            }

            result.JourneyAssignments[journeyGroup.Key] = assignmentsForJourney.OrderBy(a => a.LeaderName).ToList();
        }

        return (result, isStale);
    }

    private record LeaderPlanningRow(
        PlannerLeaderInput Leader,
        List<(PlannerJourneyInput Journey, int? RankMatched)> AssignedJourneys)
    {
        public string LeaderName => Leader.Name;
        public Dictionary<int, int> Preferences => Leader.PreferredDestinations;
    }



    private List<LeaderPlanningRow> BuildLeaderRows()
    {
        if (_request is null || _result is null) return [];

        return _request.Leaders.Select(leader => new LeaderPlanningRow(
            leader,
            _result.JourneyAssignments
                .Where(kvp => kvp.Value.Any(a => a.LeaderId == leader.Id))
                .Select(kvp => (
                    Journey: _request.Journeys.First(j => j.Id == kvp.Key),
                    RankMatched: kvp.Value.First(a => a.LeaderId == leader.Id).RankMatched
                ))
                .ToList()
        )).ToList();
    }

    private void OpenLeaderDetails(LeaderPlanningRow row)
    {
        _selectedLeaderRow = row;
        _noteModalOpen = true;
    }

    private void CloseLeaderDetails()
    {
        _noteModalOpen = false;
        _selectedLeaderRow = null;
    }

    private List<NoteModal.JourneyDetail> GetSelectedLeaderJourneys()
    {
        if (_selectedLeaderRow is null) return [];

        return _selectedLeaderRow.AssignedJourneys
            .Select(item => new NoteModal.JourneyDetail(
                item.Journey.Name,
                item.Journey.Start,
                item.Journey.End,
                item.RankMatched))
            .ToList();
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
                Id = j.Id,
                Name = j.Name,
                Start = j.Start,
                End = j.End,
                RequiredLeaders = j.RequiredLeaders,
                TravelLeaders = leaders
            };
        }).ToList();
    }
}
