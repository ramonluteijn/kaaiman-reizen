using Kaaiman_reizen.Components.Pages.Planner.Components;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Kaaiman_reizen.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerDraft : ComponentBase
{
    private const int SaveMessageDisplayMs = 5000;

    [SupplyParameterFromQuery(Name = "versionId")]
    public int? VersionId { get; set; }

    [Inject] private IPlannerDraftService _draftService { get; set; } = default!;
    [Inject] private IPlanningService _planningService { get; set; } = default!;
    [Inject] private IRuleService _ruleService { get; set; } = default!;
    [Inject] private ITravelLeaderService _travelLeaderService { get; set;  } = default!;
    [Inject] private ISnackbar _snackbar { get; set; } = default!;
    [Inject] private IUserTimezoneService UserTimezoneService { get; set; } = default!;

    private int _selectedYear = DateTime.UtcNow.Year;
    private PlannerDraftRequest? _request;
    private PlannerDraftResult? _result;
    private bool _loading = true;
    private bool _isGenerating = false;
    private bool _isSaving = false;
    private string? _saveMessage;
    private Severity _saveMessageSeverity = Severity.Info;
    private CancellationTokenSource? _saveMessageCts;
    private Data.Rules.CheckRules.RuleSettings _ruleSettings = Data.Rules.CheckRules.GetDefaultSettings();
    private bool _drawerOpen = false;
    private JourneyViewModel? _selectedJourney;
    private List<LeaderCandidate> _selectedCandidates = [];
    private bool _sidebarOpen = true;
    private bool _noteModalOpen;
    private LeaderPlanningRow? _selectedLeaderRow;
    private bool _preferenceChangesDetected = false;
    public CalendarModes selectedMode = CalendarModes.JourneyMode;
    private IReadOnlyList<TravelLeader> _availibilityPeriods = [];
    private bool _archiveDialogOpen;

    private bool CanPublish =>
        _request is not null && _result is not null && _result.IsSuccess &&
        _request.Journeys.All(j =>
            _result.JourneyAssignments.TryGetValue(j.Id, out var asgns) &&
            asgns.Count >= j.RequiredLeaders);

    protected override async Task OnInitializedAsync()
    {
        await LoadDataForYearAsync(_selectedYear);
        _availibilityPeriods = await BuildCalendarAvailibilityPeriods();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await UserTimezoneService.EnsureLoadedAsync();
    }

    private async Task<DateTime> GetUserLocalNowAsync()
    {
        await UserTimezoneService.EnsureLoadedAsync();
        return UserTimezoneService.ToUserLocal(DateTime.UtcNow);
    }

    private async Task LoadDataForYearAsync(int year)
    {
        _loading = true;
        _result = null;
        _request = await _draftService.BuildRequestAsync(year);
        var rules = await _ruleService.GetRulesAsync();
        _ruleSettings = Data.Rules.CheckRules.FromRules(rules);
        var drafts = await _planningService.GetDraftsAsync(year);
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

        var selectedDraft = await _planningService.GetPlanningVersionByIdAsync(planningVersionId);
        if (selectedDraft is null || selectedDraft.IsPublished)
        {
            SetSaveMessage($"Concept met id {planningVersionId} is niet gevonden.", Severity.Warning);
            _snackbar.Add(_saveMessage, _saveMessageSeverity);
            return false;
        }

        var (mapped, isStale) = MapToDraftResult(selectedDraft);
        _result = mapped;
        _preferenceChangesDetected = !isStale && DetectPreferenceChanges();
        return isStale;
    }

    private async Task RegeneratePlanningAsync(bool autoUpdated = false)
    {
        if (_request is null) return;

        _isGenerating = true;
        _result = null;
        _preferenceChangesDetected = false;
        ClearSaveMessage();
        StateHasChanged();

        await Task.Delay(50);
        _result = await Task.Run(() => _draftService.GenerateDraft(_request));
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
        public List<PreferredDestinationDisplayInput> Preferences => Leader.PreferredDestinationDetails;
    }



    private List<LeaderPlanningRow> BuildLeaderRows()
    {
        if (_request is null || _result is null) return [];

        return _request.AllActiveLeaders.Select(leader => new LeaderPlanningRow(
            leader,
            _result.JourneyAssignments
                .Where(kvp => kvp.Value.Any(a => a.LeaderId == leader.Id))
                .Select(kvp => (
                    _request.Journeys.First(j => j.Id == kvp.Key),
                    kvp.Value.First(a => a.LeaderId == leader.Id).RankMatched
                ))
                .ToList()
        )).ToList();
    }

    private bool DetectPreferenceChanges()
    {
        if (_request is null || _result is null) return false;

        var draftLeaderIds = _result.JourneyAssignments.Values
            .SelectMany(l => l)
            .Select(a => a.LeaderId)
            .ToHashSet();

        // Leaders who now have preferences but weren't in the draft at all
        if (_request.Leaders.Any(l => !draftLeaderIds.Contains(l.Id)))
            return true;

        // Assigned leaders whose preference for their journey was removed
        foreach (var (journeyId, assignments) in _result.JourneyAssignments)
        {
            foreach (var assignment in assignments)
            {
                var leader = _request.Leaders.FirstOrDefault(l => l.Id == assignment.LeaderId);
                if (leader is null) continue;
                if (!leader.PreferredDestinations.ContainsKey(journeyId))
                    return true;
            }
        }

        return false;
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

    private async Task<IReadOnlyList<TravelLeader>> BuildCalendarAvailibilityPeriods()
    {
        if (_request is null || _result is null) return [];

        return await _travelLeaderService.GetJourneyAvailabilityForAllTravelLeadersAsync();
    private void OpenArchiveAvailabilityDialog() => _archiveDialogOpen = true;

    private void CloseArchiveDialog() => _archiveDialogOpen = false;

    private void HandleArchiveDialogChanged(bool isOpen) => _archiveDialogOpen = isOpen;

    private async Task ConfirmArchiveAsync()
    {
        if (_archiveDialogOpen is false)
            return;

        _archiveDialogOpen = false;
        _isSaving = true;

        try
        {
            var planningVersionId = await PlanningService.GetLatestPublishedPlanningVersionIdAsync();
            var archivedCount = await TravelLeaderService.ArchiveAndResetPreferredDestinationsAsync(planningVersionId);

            Snackbar.Add($"Beschikbaarheid gearchiveerd en gereset ({archivedCount} items).", Severity.Success);
            await LoadDataForYearAsync(_selectedYear);
        }
        catch (Exception)
        {
            Snackbar.Add("Archiveren en resetten van beschikbaarheid is mislukt.", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }
}
