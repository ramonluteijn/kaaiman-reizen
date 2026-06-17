using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Preferences;

public partial class Preferences : ComponentBase
{
    [Inject] private ITravelLeaderService LeaderService { get; set; } = default!;
    [Inject] private IJourneyService JourneyService { get; set; } = default!;
    [Inject] private IPlanningRoundService RoundService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider _authProvider { get; set; } = default!;
    [Inject] private ISnackbar _snackbar { get; set; } = default!;

    [Parameter] public int? Id { get; set; }

    // Profile form state
    private TravelLeader _model = new();
    private ProfileFormModel _profileForm = new();
    private bool _loading = true;
    private bool _notFound = false;
    private bool _isSaving = false;
    private List<Journey> _journeys = new();

    // Round participation state
    private List<PlanningRoundParticipation> _participations = [];
    private int? _expandedParticipationId;
    private int?[] _roundPreferredJourneyIds = new int?[3];
    private HashSet<int> _roundAvailableJourneyIds = new();
    private List<Journey> _journeysForRound = [];
    private bool _submittingRound;
    private string? _roundSuccessMessage;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _notFound = false;

        TravelLeader? item = null;

        var authState = await _authProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        bool isPlanner = user.IsInRole("Planner");

        if (isPlanner && Id.HasValue && Id.Value > 0)
        {
            item = await LeaderService.GetTravelLeaderByIdAsync(Id.Value);
        }
        else
        {
            var leaderId = user.FindFirst("TravelLeaderId")?.Value;
            if (!string.IsNullOrEmpty(leaderId) && int.TryParse(leaderId, out int id))
                item = await LeaderService.GetTravelLeaderByIdAsync(id);
        }

        if (item == null)
        {
            _notFound = true;
            _loading = false;
            return;
        }

        _model = item;
        _profileForm = ProfileFormModel.FromTravelLeader(item);

        var allJourneys = await JourneyService.GetJourneysAsync();
        _journeys = allJourneys
            .Where(j => j.BookingStatus == BookingStatus.Huidig)
            .OrderBy(j => j.Start)
            .ToList();

        _participations = (await RoundService.GetParticipationsForLeaderAsync(_model.Id)).ToList();

        _loading = false;
    }

    // ── Round preferences ────────────────────────────────────────────

    private void ExpandRound(PlanningRoundParticipation participation, List<Journey> allJourneys)
    {
        _expandedParticipationId = participation.Id;
        _roundSuccessMessage = null;

        var round = participation.PlanningRound;
        _journeysForRound = allJourneys
            .Where(j => j.BookingStatus == BookingStatus.Huidig
                     && j.Start >= round.StartDate
                     && j.Start <= round.EndDate)
            .OrderBy(j => j.Start)
            .ToList();

        _roundPreferredJourneyIds = new int?[3];
        _roundAvailableJourneyIds = new HashSet<int>();

        foreach (var pref in participation.Preferences)
        {
            if (pref.Rank >= 1 && pref.Rank <= 3)
                _roundPreferredJourneyIds[pref.Rank - 1] = pref.JourneyId;
            else if (pref.Rank == 0)
                _roundAvailableJourneyIds.Add(pref.JourneyId);
        }
    }

    private void CollapseRound()
    {
        _expandedParticipationId = null;
    }

    private void SetRoundPreference(int rank, ChangeEventArgs e)
    {
        var val = e.Value?.ToString();
        _roundPreferredJourneyIds[rank] = string.IsNullOrEmpty(val) ? null : int.Parse(val);
    }

    private string GetDeadlineClass(DateTime deadline)
    {
        var days = (int)(deadline.Date - DateTime.UtcNow.Date).TotalDays;
        if (days < 0) return "text-danger";
        if (days <= 7) return "text-warning";
        return "text-muted";
    }

    private string GetDeadlineLabel(DateTime deadline)
    {
        var days = (int)(deadline.Date - DateTime.UtcNow.Date).TotalDays;
        if (days < 0) return "· Deadline verlopen";
        if (days == 0) return "· Deadline vandaag";
        return $"· Deadline over {days} dag{(days == 1 ? "" : "en")}";
    }

    private void ToggleRoundAvailable(int journeyId, bool isChecked)
    {
        if (isChecked) _roundAvailableJourneyIds.Add(journeyId);
        else _roundAvailableJourneyIds.Remove(journeyId);
    }

    private async Task HandleRoundSubmit(int participationId)
    {
        _submittingRound = true;

        var preferences = new List<(int JourneyId, int Rank)>();

        for (int i = 0; i < 3; i++)
        {
            if (_roundPreferredJourneyIds[i].HasValue)
                preferences.Add((_roundPreferredJourneyIds[i]!.Value, i + 1));
        }

        var top3Ids = _roundPreferredJourneyIds.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        foreach (var jid in _roundAvailableJourneyIds)
        {
            if (!top3Ids.Contains(jid))
                preferences.Add((jid, 0));
        }

        await RoundService.SavePreferencesAsync(participationId, preferences);

        _participations = (await RoundService.GetParticipationsForLeaderAsync(_model.Id)).ToList();
        _expandedParticipationId = null;
        _roundSuccessMessage = "Voorkeuren ingediend!";
        _submittingRound = false;
    }

    // ── Global profile form ──────────────────────────────────────────

    private async Task HandleSubmit()
    {
        _isSaving = true;
        try
        {
            await LeaderService.UpdateProfileAsync(
                _model.Id,
                _profileForm.AmountOfTrips,
                _profileForm.MinTrips,
                _profileForm.MaxTrips,
                _profileForm.Note,
                _profileForm.IsActive);

            _model.AmountOfTrips = _profileForm.AmountOfTrips;
            _model.MinTrips = _profileForm.MinTrips;
            _model.MaxTrips = _profileForm.MaxTrips;
            _model.Note = _profileForm.Note;
            _model.IsActive = _profileForm.IsActive;

            _snackbar.Add("Profiel opgeslagen.", Severity.Success);
        }
        catch
        {
            _snackbar.Add("Opslaan mislukt. Probeer het opnieuw.", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private sealed class ProfileFormModel : IValidatableObject
    {
        [Required(ErrorMessage = "Aantal reizen gereisd is verplicht.")]
        [Range(0, int.MaxValue, ErrorMessage = "Aantal reizen moet groter of gelijk aan 0 zijn.")]
        public int? AmountOfTrips { get; set; }

        [Required(ErrorMessage = "Minimaal aantal reizen per jaar is verplicht.")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimaal aantal reizen moet groter of gelijk aan 0 zijn.")]
        public int? MinTrips { get; set; }

        [Required(ErrorMessage = "Maximaal aantal reizen per jaar is verplicht.")]
        [Range(0, int.MaxValue, ErrorMessage = "Maximaal aantal reizen moet groter of gelijk aan 0 zijn.")]
        public int? MaxTrips { get; set; }

        public string Note { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinTrips.HasValue && MaxTrips.HasValue && MinTrips > MaxTrips)
            {
                yield return new ValidationResult(
                    "Minimaal aantal reizen mag niet groter zijn dan maximaal aantal reizen.",
                    [nameof(MinTrips), nameof(MaxTrips)]);
            }
        }

        public static ProfileFormModel FromTravelLeader(TravelLeader leader) => new()
        {
            AmountOfTrips = leader.AmountOfTrips,
            MinTrips = leader.MinTrips,
            MaxTrips = leader.MaxTrips,
            Note = leader.Note,
            IsActive = leader.IsActive,
        };
    }
}
