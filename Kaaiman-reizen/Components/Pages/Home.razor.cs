using Kaaiman_reizen.Components.Pages.Planner.Components;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Exports;
using Kaaiman_reizen.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using QuestPDF.Fluent;
using static Kaaiman_reizen.Data.Services.TravelLeaderService;

namespace Kaaiman_reizen.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IPlanningService PlanningService { get; set; } = default!;
    [Inject] private ITravelLeaderService TravelLeaderService { get; set; } = default!;
    [Inject] private IJourneyService JourneyService { get; set; } = default!;
    [Inject] private IPlanningRoundService RoundService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private Microsoft.AspNetCore.Identity.UserManager<Kaaiman_reizen.Data.Identity.ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private INotificationService NotificationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _loading = true;
    private bool _isPlanner;
    private bool _isReisleider;

    private List<Notification> _notifications = [];
    private string _currentUserId = string.Empty;

    // Planning rounds + journeys (voor stappenplan)
    private IReadOnlyList<PlanningRound> _rounds = [];
    private List<Journey> _allJourneys = [];

    // Legacy data voor de kaarten onder het stappenplan
    private int _selectedYear = DateTime.UtcNow.Year;
    private bool _publishedPlanning;
    private bool _publishedPlanningIsComplete;
    private List<Journey> _plannedJourneysWithTravelLeaders = [];
    private List<TravelLeader> _travelLeadersWithoutJourneys = [];
    private List<TravelLeader> _travelLeadersWithNotes = [];
    private List<Journey> _journeysWithoutTravelLeaders = [];

    // Reisleider-weergave
    private List<Journey> _publishedJourneys = [];

    private int _userTimezoneOffsetMinutes;
    private bool _userTimezoneOffsetLoaded;

    protected override async Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;

        if (user.Identity?.IsAuthenticated is not true)
        {
            Navigation.NavigateTo("/Account/Login");
            return;
        }

        _isPlanner = user.IsInRole("Planner");
        _isReisleider = user.IsInRole("Reisleider");

        var appUser = await UserManager.GetUserAsync(user);
        if (appUser != null)
        {
            _currentUserId = appUser.Id;
            _notifications = await NotificationService.GetUnreadNotificationsAsync(_currentUserId);
        }

        if (_isPlanner)
        {
            _rounds = await RoundService.GetAllAsync();

            var journeys = await JourneyService.GetJourneysAsync();
            _allJourneys = journeys.Where(j => j.BookingStatus == BookingStatus.Huidig).ToList();

            _travelLeadersWithoutJourneys = await TravelLeaderService.GetTravelLeadersWithoutJourneysAsync(_selectedYear);
            _travelLeadersWithNotes = await TravelLeaderService.GetTravelLeadersWithNotesAsync();
            _journeysWithoutTravelLeaders = await TravelLeaderService.GetJourneysWithoutTravelLeadersAsync(_selectedYear);

            _publishedPlanning = PlanningService.PublishedPlanningExists();
            _publishedPlanningIsComplete = await PlanningService.IsPublishedPlanningCompleteAsync(_selectedYear);
            _plannedJourneysWithTravelLeaders = await PlanningService.GetAllJourneysWithTravelLeadersFromLatestPublishedPlanning();
        }

        if (_isReisleider)
        {
            var journeys = await JourneyService.GetJourneysWithPublishedPlanningAsync(_selectedYear);
            _publishedJourneys = journeys.ToList();
        }

        _loading = false;
    }

    // ── Stappenplan ───────────────────────────────────────────────────────

    private StappenplanModel BuildStappenplan(PlanningRound round)
    {
        var now = DateTime.UtcNow;
        var deadlinePassed = now > round.PreferenceDeadline;

        var journeysInRound = _allJourneys
            .Where(j => j.Start >= round.StartDate && j.Start <= round.EndDate)
            .ToList();
        var journeyCount = journeysInRound.Count;

        var totalParticipants = round.Participations.Count;
        var submittedCount = round.Participations.Count(p => p.Status == ParticipationStatus.Submitted);

        var hasDraft = round.Versions.Any(v => !v.IsPublished);
        var isPublished = round.Versions.Any(v => v.IsPublished);
        var draftIsStale = IsDraftStale(round);

        var journeyIdsWithPrefs = round.Participations
            .SelectMany(p => p.Preferences)
            .Select(pref => pref.JourneyId)
            .ToHashSet();
        var journeysWithoutPrefs = journeysInRound.Count(j => !journeyIdsWithPrefs.Contains(j.Id));

        // Stap 1: Reizen aanmaken
        var step1Status = journeyCount > 0 ? StapStatus.Voltooid : StapStatus.Huidig;
        var step1 = new PlanStap
        {
            Nummer = 1,
            Titel = "Reizen aanmaken",
            Status = step1Status,
            Sublabel = journeyCount > 0 ? $"{journeyCount} reizen" : "0 reizen",
            SublabelStijl = journeyCount > 0 ? SublabelStijl.GroenBadge : SublabelStijl.OranjeText,
            KnopTekst = "Reizen beheren",
            KnopVariant = KnopVariant.Outlined,
            KnopHref = "/journeys"
        };

        // Stap 2: Voorkeuren uitvragen
        StapStatus step2Status;
        if (totalParticipants > 0 && submittedCount == totalParticipants)
            step2Status = StapStatus.Voltooid;
        else if (!deadlinePassed)
            step2Status = StapStatus.Huidig;
        else
            step2Status = StapStatus.Aandacht;

        var step2 = new PlanStap
        {
            Nummer = 2,
            Titel = "Voorkeuren uitvragen",
            Status = step2Status,
            Sublabel = $"{submittedCount} / {totalParticipants} ingediend",
            SublabelStijl = step2Status switch
            {
                StapStatus.Voltooid => SublabelStijl.GroenBadge,
                StapStatus.Aandacht => SublabelStijl.RoodText,
                _ => SublabelStijl.OranjeText
            },
            KnopTekst = step2Status == StapStatus.Huidig ? "Herinner" : null,
            KnopVariant = KnopVariant.Primair,
            KnopHref = $"/planner/rounds/{round.Id}/draft"
        };

        // Stap 3: Voorkeuren controleren
        StapStatus step3Status;
        string step3Sublabel;
        SublabelStijl step3SublabelStijl;

        if (!deadlinePassed)
        {
            step3Status = StapStatus.Toekomstig;
            step3Sublabel = $"deadline {round.PreferenceDeadline:d MMM}";
            step3SublabelStijl = SublabelStijl.GrijsText;
        }
        else if (journeysWithoutPrefs == 0)
        {
            step3Status = StapStatus.Voltooid;
            step3Sublabel = "Volledig";
            step3SublabelStijl = SublabelStijl.GroenBadge;
        }
        else
        {
            step3Status = StapStatus.Aandacht;
            step3Sublabel = $"{journeysWithoutPrefs} zonder voorkeur";
            step3SublabelStijl = SublabelStijl.RoodText;
        }

        var step3 = new PlanStap
        {
            Nummer = 3,
            Titel = "Voorkeuren controleren",
            Status = step3Status,
            Sublabel = step3Sublabel,
            SublabelStijl = step3SublabelStijl,
            KnopTekst = deadlinePassed ? "Open ronde" : null,
            KnopVariant = KnopVariant.Outlined,
            KnopHref = $"/planner/rounds/{round.Id}/draft"
        };

        // Stap 4: Planning genereren
        StapStatus step4Status;
        string step4Sublabel;
        SublabelStijl step4SublabelStijl;
        string? step4KnopTekst;
        KnopVariant step4KnopVariant;

        if (isPublished || (hasDraft && !draftIsStale))
        {
            step4Status = StapStatus.Voltooid;
            step4Sublabel = "concept aangemaakt";
            step4SublabelStijl = SublabelStijl.GroenBadge;
            step4KnopTekst = isPublished ? null : "Open";
            step4KnopVariant = KnopVariant.Outlined;
        }
        else if (draftIsStale)
        {
            step4Status = StapStatus.Aandacht;
            step4Sublabel = "verouderd concept";
            step4SublabelStijl = SublabelStijl.OranjeText;
            step4KnopTekst = "Opnieuw genereren";
            step4KnopVariant = KnopVariant.Gevaar;
        }
        else if (step3Status == StapStatus.Voltooid)
        {
            step4Status = StapStatus.Huidig;
            step4Sublabel = "nog geen concept";
            step4SublabelStijl = SublabelStijl.GrijsText;
            step4KnopTekst = "Genereer";
            step4KnopVariant = KnopVariant.Primair;
        }
        else
        {
            step4Status = StapStatus.Toekomstig;
            step4Sublabel = "nog geen concept";
            step4SublabelStijl = SublabelStijl.GrijsText;
            step4KnopTekst = null;
            step4KnopVariant = KnopVariant.Outlined;
        }

        var step4 = new PlanStap
        {
            Nummer = 4,
            Titel = "Planning genereren",
            Status = step4Status,
            Sublabel = step4Sublabel,
            SublabelStijl = step4SublabelStijl,
            KnopTekst = step4KnopTekst,
            KnopVariant = step4KnopVariant,
            KnopHref = $"/planner/rounds/{round.Id}/draft"
        };

        // Stap 5: Publiceren
        StapStatus step5Status;
        if (isPublished)
            step5Status = StapStatus.Voltooid;
        else if (step4Status == StapStatus.Voltooid)
            step5Status = StapStatus.Huidig;
        else
            step5Status = StapStatus.Toekomstig;

        var step5 = new PlanStap
        {
            Nummer = 5,
            Titel = "Publiceren",
            Status = step5Status,
            Sublabel = isPublished ? "gepubliceerd" : $"deadline {round.PublicationDeadline:d MMM}",
            SublabelStijl = isPublished ? SublabelStijl.GroenBadge : SublabelStijl.GrijsText,
            KnopTekst = step5Status != StapStatus.Toekomstig ? "Open" : null,
            KnopVariant = KnopVariant.Outlined,
            KnopHref = $"/planner/rounds/{round.Id}/draft"
        };

        var stappen = new List<PlanStap> { step1, step2, step3, step4, step5 };
        var voltooideStappen = stappen.Count(s => s.Status == StapStatus.Voltooid);
        var voortgang = (voltooideStappen * 100) / stappen.Count;
        var huidigStap = stappen
            .Select((s, i) => (s, i + 1))
            .FirstOrDefault(x => x.s.Status != StapStatus.Voltooid).Item2;
        if (huidigStap == 0) huidigStap = stappen.Count;

        return new StappenplanModel
        {
            RondeNaam = round.Name,
            Seizoen = round.Year,
            Voortgang = voortgang,
            HuidigStap = huidigStap,
            Stappen = stappen
        };
    }

    private bool IsDraftStale(PlanningRound round)
    {
        var draft = round.Versions
            .Where(v => !v.IsPublished)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        if (draft is null) return false;
        return round.Participations.Any(p => p.SubmittedAt.HasValue && p.SubmittedAt > draft.CreatedAt);
    }

    private async Task VerifyLeadersAsync(PlanningRound round)
    {
        await RoundService.VerifyLeadersAsync(round.Id);
        round.LeadersVerifiedAt = DateTime.UtcNow;
    }

    // ── Notificaties ──────────────────────────────────────────────────────

    private async Task MarkNotificationAsRead(Notification notification)
    {
        await NotificationService.MarkAsReadAsync(notification.Id, _currentUserId);
        _notifications.Remove(notification);
    }

    // ── Timezone ──────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await EnsureUserTimezoneOffsetAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task EnsureUserTimezoneOffsetAsync()
    {
        if (_userTimezoneOffsetLoaded) return;

        try
        {
            _userTimezoneOffsetMinutes = await JS.InvokeAsync<int>("kaaimanDateTime.getTimezoneOffsetMinutes");
        }
        catch
        {
            _userTimezoneOffsetMinutes = 0;
        }
        finally
        {
            _userTimezoneOffsetLoaded = true;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string FormatTravelLeaders(Journey journey)
    {
        var leaders = journey.TravelLeaders ?? [];
        if (leaders.Count == 0) return "-";
        return string.Join("; ", leaders.OrderBy(l => l.Name).Select(l => l.Name));
    }

    private async Task HandlePrintPdf()
    {
        try
        {
            var userLocalPrintDate = DateDisplay.FormatDate(DateDisplay.ToUserLocal(DateTime.UtcNow, _userTimezoneOffsetMinutes));
            var document = new PlanningDocument(_plannedJourneysWithTravelLeaders, userLocalPrintDate);
            byte[] pdfBytes = document.GeneratePdf();
            using var stream = new MemoryStream(pdfBytes);
            using var streamRef = new DotNetStreamReference(stream);
            await JS.InvokeVoidAsync("downloadFileFromStream", "Planning.pdf", streamRef);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
