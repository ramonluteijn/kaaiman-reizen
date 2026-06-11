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

public partial class PlannerRoundDraft : ComponentBase
{
    private const int SaveMessageDisplayMs = 5000;

    [Parameter] public int RoundId { get; set; }

    [Inject] private IPlanningRoundService _roundService { get; set; } = default!;
    [Inject] private IPlannerDraftService _draftService { get; set; } = default!;
    [Inject] private IPlanningService _planningService { get; set; } = default!;
    [Inject] private IRuleService _ruleService { get; set; } = default!;
    [Inject] private ISnackbar _snackbar { get; set; } = default!;
    [Inject] private NavigationManager _nav { get; set; } = default!;
    [Inject] private IUserTimezoneService UserTimezoneService { get; set; } = default!;

    private PlanningRound? _round;
    private PlannerDraftRequest? _request;
    private PlannerDraftResult? _result;
    private bool _loading = true;
    private bool _roundNotFound = false;
    private bool _isGenerating = false;
    private bool _isSaving = false;
    private string? _saveMessage;
    private Severity _saveMessageSeverity = Severity.Info;
    private CancellationTokenSource? _saveMessageCts;
    private Data.Rules.CheckRules.RuleSettings _ruleSettings = Data.Rules.CheckRules.GetDefaultSettings();
    private bool _drawerOpen = false;
    private JourneyViewModel? _selectedJourney;
    private List<LeaderCandidate> _selectedCandidates = [];
    private bool _sidebarLeaderOpen = true;
    private bool _sidebarJourneyOpen = false;
    private DateOnly? _jumpToDate;
    private bool _noteModalOpen;
    private LeaderPlanningRow? _selectedLeaderRow;
    private bool _preferenceChangesDetected = false;
    public CalendarModes selectedMode = CalendarModes.JourneyMode;
    private IReadOnlyList<TravelLeader> _availibilityPeriods = [];

    private bool CanPublish =>
        _request is not null && _result is not null && _result.IsSuccess &&
        _request.Journeys.All(j =>
            _result.JourneyAssignments.TryGetValue(j.Id, out var asgns) &&
            asgns.Count >= j.RequiredLeaders);

    private int SubmittedCount => _round?.Participations.Count(p => p.Status == ParticipationStatus.Submitted) ?? 0;
    private int UnavailableCount => _round?.Participations.Count(p => p.Status == ParticipationStatus.Unavailable) ?? 0;
    private int TotalCount => _round?.Participations.Count ?? 0;
    private HashSet<int> UnavailableLeaderIds => _round?.Participations
        .Where(p => p.Status == ParticipationStatus.Unavailable)
        .Select(p => p.TravelLeaderId)
        .ToHashSet() ?? [];

    private int DaysUntilPreferenceDeadline =>
        (int)(_round!.PreferenceDeadline.Date - DateTime.UtcNow.Date).TotalDays;

    private Color PreferenceDeadlineColor
    {
        get
        {
            if (_round is null) return Color.Default;
            var days = DaysUntilPreferenceDeadline;
            if (days < 0) return Color.Error;
            if (days <= 7) return Color.Warning;
            return Color.Default;
        }
    }

    private Color ParticipationColor
    {
        get
        {
            if (_round is null || TotalCount == 0) return Color.Default;
            var ratio = (double)(SubmittedCount + UnavailableCount) / TotalCount;
            if (ratio >= 1.0) return Color.Success;
            if (ratio >= 0.5) return Color.Warning;
            return Color.Error;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadDataForRoundAsync();
        _availibilityPeriods = BuildAvailabilityFromRound();
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

    private async Task LoadDataForRoundAsync()
    {
        _loading = true;
        _result = null;

        _round = await _roundService.GetByIdAsync(RoundId);
        if (_round is null)
        {
            _roundNotFound = true;
            _loading = false;
            return;
        }

        _request = await _draftService.BuildRequestAsync(_round);
        var rules = await _ruleService.GetRulesAsync();
        _ruleSettings = Data.Rules.CheckRules.FromRules(rules);

        var drafts = await _planningService.GetDraftsByRoundAsync(RoundId);
        var draftToLoad = drafts.FirstOrDefault();
        _loading = false;
        StateHasChanged();

        if (draftToLoad is not null)
        {
            bool isStale = await LoadVersionByIdAsync(draftToLoad.Id, expectPublished: false);
            if (isStale)
                await RegeneratePlanningAsync(autoUpdated: true);
        }
        else
        {
            var published = await _planningService.GetPublishedByRoundAsync(RoundId);
            if (published is not null)
            {
                var (mapped, _) = MapToDraftResult(published);
                _result = mapped;
            }
            else
            {
                await RegeneratePlanningAsync(autoUpdated: false);
            }
        }
    }

    private async Task<bool> LoadVersionByIdAsync(int planningVersionId, bool expectPublished)
    {
        if (_request is null) return false;

        var version = await _planningService.GetPlanningVersionByIdAsync(planningVersionId);
        if (version is null)
        {
            SetSaveMessage($"Versie met id {planningVersionId} is niet gevonden.", Severity.Warning);
            return false;
        }

        var (mapped, isStale) = MapToDraftResult(version);
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

        if (!autoUpdated)
        {
            var freshRound = await _roundService.GetByIdAsync(RoundId);
            if (freshRound is not null)
            {
                _round = freshRound;
                _request = await _draftService.BuildRequestAsync(_round);
                _availibilityPeriods = BuildAvailabilityFromRound();
            }
        }

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
                result.JourneyWarnings[journeyGroup.Key] = "Deze reis valt buiten de datumreeks van deze ronde.";
                isStale = true;
                continue;
            }

            var assignmentsForJourney = new List<JourneyAssignmentResult>();

            foreach (var assignment in journeyGroup)
            {
                var isAssigneeUnavailable = _round!.Participations
                    .Any(p => p.TravelLeaderId == assignment.TravelLeaderId
                           && p.Status == ParticipationStatus.Unavailable);

                var leader = _request.Leaders.FirstOrDefault(l => l.Id == assignment.TravelLeaderId);
                if (leader is null || isAssigneeUnavailable)
                {
                    result.JourneyWarnings[journey.Id] = $"Let op: {assignment.TravelLeader?.Name ?? "Reisleider"} is automatisch verwijderd omdat deze inactief is, geen voorkeuren heeft of zich heeft afgemeld.";
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

        if (_request.Leaders.Any(l => !draftLeaderIds.Contains(l.Id)))
            return true;

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

    private void GoToMonth(JourneyViewModel journey)
    {
        _jumpToDate = journey.Start;
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

    private IReadOnlyList<TravelLeader> BuildAvailabilityFromRound()
    {
        if (_round is null || _request is null) return [];

        var leaderNames = _request.AllActiveLeaders.ToDictionary(l => l.Id, l => l.Name);

        return _round.Participations
            .Where(p => p.Preferences.Any())
            .Select(p =>
            {
                leaderNames.TryGetValue(p.TravelLeaderId, out var name);
                return new TravelLeader
                {
                    Id = p.TravelLeaderId,
                    Name = name ?? string.Empty,
                    PreferredDestinations = p.Preferences
                        .Select(pref => new PreferredDestination
                        {
                            JourneyId = pref.JourneyId,
                            Journey = pref.Journey,
                            Rank = pref.Rank
                        }).ToList()
                };
            }).ToList();
    }
}
