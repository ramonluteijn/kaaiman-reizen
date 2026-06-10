using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerRounds : ComponentBase
{
    [Inject] private IPlanningRoundService RoundService { get; set; } = default!;
    [Inject] private IJourneyService JourneyService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<PlanningRound> _rounds = [];
    private List<Journey> _allJourneys = [];
    private bool _loading = true;
    private bool _isDeleteModalOpen;
    private PlanningRound? _roundPendingDelete;
    private bool _isDeleting;

    protected override async Task OnInitializedAsync()
    {
        var rounds = await RoundService.GetAllAsync();
        _rounds = rounds;
        var journeys = await JourneyService.GetJourneysAsync();
        _allJourneys = journeys.Where(j => j.BookingStatus == BookingStatus.Huidig).ToList();
        _loading = false;
    }

    private int JourneyCount(PlanningRound round) =>
        _allJourneys.Count(j => j.Start >= round.StartDate && j.Start <= round.EndDate);

    private int SubmittedCount(PlanningRound round) =>
        round.Participations.Count(p => p.Status == ParticipationStatus.Submitted);

    private RoundStatus GetStatus(PlanningRound round)
    {
        if (round.Versions.Any(v => v.IsPublished)) return RoundStatus.Gepubliceerd;
        if (DateTime.UtcNow < round.PreferenceDeadline) return RoundStatus.Open;
        return RoundStatus.VoorkeurenGesloten;
    }

    private string StatusBadgeClass(RoundStatus status) => status switch
    {
        RoundStatus.Open => "bg-success",
        RoundStatus.VoorkeurenGesloten => "bg-warning text-dark",
        RoundStatus.Gepubliceerd => "bg-primary",
        _ => "bg-secondary"
    };

    private string StatusLabel(RoundStatus status) => status switch
    {
        RoundStatus.Open => "Open",
        RoundStatus.VoorkeurenGesloten => "Voorkeuren gesloten",
        RoundStatus.Gepubliceerd => "Gepubliceerd",
        _ => ""
    };

    private string DeadlineLabel(DateTime deadline)
    {
        var days = (int)(deadline.Date - DateTime.UtcNow.Date).TotalDays;
        if (days < 0) return "verlopen";
        if (days == 0) return "vandaag";
        return $"over {days} dag{(days == 1 ? "" : "en")}";
    }

    private bool HasPublishedVersion(PlanningRound round) =>
        round.Versions.Any(v => v.IsPublished);

    private string DeleteModalMessage => _roundPendingDelete is null
        ? string.Empty
        : HasPublishedVersion(_roundPendingDelete)
            ? $"Weet je zeker dat je '{_roundPendingDelete.Name}' wilt verwijderen? " +
              "De gepubliceerde planning blijft bewaard maar wordt losgekoppeld van deze ronde. " +
              "Voorkeuren en deelname worden wel verwijderd."
            : $"Weet je zeker dat je '{_roundPendingDelete.Name}' wilt verwijderen? " +
              "Alle voorkeuren en conceptplanningen worden ook verwijderd.";

    private void OpenDeleteModal(PlanningRound round)
    {
        _roundPendingDelete = round;
        _isDeleteModalOpen = true;
    }

    private void CloseDeleteModal()
    {
        _isDeleteModalOpen = false;
        _roundPendingDelete = null;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_roundPendingDelete is null) return;

        _isDeleting = true;
        var id = _roundPendingDelete.Id;

        try
        {
            await RoundService.DeleteAsync(id);
            _rounds = _rounds.Where(r => r.Id != id).ToList();
        }
        finally
        {
            _isDeleting = false;
            _isDeleteModalOpen = false;
            _roundPendingDelete = null;
        }
    }

    private enum RoundStatus { Open, VoorkeurenGesloten, Gepubliceerd }
}
