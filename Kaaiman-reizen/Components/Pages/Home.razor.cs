using Kaaiman_reizen.Components.Pages.Planner.Components;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Data.Dtos;
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

    private IReadOnlyList<PlanningRound> _rounds = [];
    private List<Journey> _allJourneys = [];
    private HashSet<int> _expandedRounds = [];

    private int _selectedYear = DateTime.UtcNow.Year;
    private bool _publishedPlanning;
    private List<Journey> _plannedJourneysWithTravelLeaders = [];

    private List<Journey> _publishedJourneys = [];

    private int _userTimezoneOffsetMinutes;
    private bool _userTimezoneOffsetLoaded;

    private List<Notification> _notifications = [];
    private string _currentUserId = string.Empty;

    private IReadOnlyList<PlanningRound> _rounds = [];
    private List<TravelLeader> _travelLeadersWithoutJourneys = [];
    private List<ParticipationNoteEntry> _travelLeadersWithNotes = [];
    private List<Journey> _journeysWithoutTravelLeaders = [];
    private List<OverlapData> _travelLeadersWithOverlappingJourneys = [];

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
            _travelLeadersWithoutJourneys = await TravelLeaderService.GetTravelLeadersWithoutJourneysAsync(_selectedYear);
            _travelLeadersWithNotes = (await RoundService.GetParticipationNotesAsync()).ToList();
            _journeysWithoutTravelLeaders = await TravelLeaderService.GetJourneysWithoutTravelLeadersAsync(_selectedYear);
            _travelLeadersWithOverlappingJourneys = await TravelLeaderService.GetTravelLeadersWithOverlappingJourneys();

            _expandedRounds = _rounds
                .Where(r => !r.Versions.Any(v => v.IsPublished))
                .Select(r => r.Id)
                .ToHashSet();

            var journeys = await JourneyService.GetJourneysAsync();
            _allJourneys = journeys.Where(j => j.BookingStatus == BookingStatus.Huidig).ToList();

            _publishedPlanning = PlanningService.PublishedPlanningExists();
            _plannedJourneysWithTravelLeaders = await PlanningService.GetAllJourneysWithTravelLeadersFromLatestPublishedPlanning();
        }

        if (_isReisleider)
        {
            var journeys = await JourneyService.GetJourneysWithPublishedPlanningAsync(_selectedYear);
            _publishedJourneys = journeys.ToList();
        }

        _loading = false;
    }

    // ── Planning round steps ──────────────────────────────────────────────

    private void ToggleRound(int id)
    {
        if (!_expandedRounds.Remove(id)) _expandedRounds.Add(id);
    }

    private StepPlanModel BuildPlanningRoundSteps(PlanningRound round)
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

        // Step 1: Reizen aanmaken
        var step1Status = journeyCount > 0 ? StepStatus.Completed : StepStatus.Current;
        var step1 = new PlanStep
        {
            Number = 1,
            Title = "Reizen aanmaken",
            Status = step1Status,
            Sublabel = journeyCount > 0 ? $"{journeyCount} reizen" : "0 reizen",
            SublabelStyle = journeyCount > 0 ? SublabelStyle.GreenBadge : SublabelStyle.OrangeText,
            ButtonText = "Reizen beheren",
            ButtonVariant = ButtonVariant.Outlined,
            ButtonHref = "/journeys"
        };

        // Step 2: Voorkeuren uitvragen
        StepStatus step2Status;
        if (totalParticipants > 0 && submittedCount == totalParticipants)
            step2Status = StepStatus.Completed;
        else if (!deadlinePassed)
            step2Status = StepStatus.Current;
        else
            step2Status = StepStatus.Attention;

        var step2 = new PlanStep
        {
            Number = 2,
            Title = "Voorkeuren uitvragen",
            Status = step2Status,
            Sublabel = $"{submittedCount} / {totalParticipants} ingediend",
            SublabelStyle = step2Status switch
            {
                StepStatus.Completed => SublabelStyle.GreenBadge,
                StepStatus.Attention => SublabelStyle.RedText,
                _                    => SublabelStyle.OrangeText
            },
            ButtonText = step2Status == StepStatus.Current ? "Herinner" : null,
            ButtonVariant = ButtonVariant.Primary,
            ButtonHref = $"/planner/rounds/{round.Id}/draft"
        };

        // Step 3: Voorkeuren controleren
        StepStatus step3Status;
        string step3Sublabel;
        SublabelStyle step3SublabelStyle;

        if (!deadlinePassed)
        {
            step3Status = StepStatus.Future;
            step3Sublabel = $"deadline {round.PreferenceDeadline:d MMM}";
            step3SublabelStyle = SublabelStyle.GreyText;
        }
        else if (journeysWithoutPrefs == 0)
        {
            step3Status = StepStatus.Completed;
            step3Sublabel = "Volledig";
            step3SublabelStyle = SublabelStyle.GreenBadge;
        }
        else
        {
            step3Status = StepStatus.Attention;
            step3Sublabel = $"{journeysWithoutPrefs} zonder voorkeur";
            step3SublabelStyle = SublabelStyle.RedText;
        }

        var step3 = new PlanStep
        {
            Number = 3,
            Title = "Voorkeuren controleren",
            Status = step3Status,
            Sublabel = step3Sublabel,
            SublabelStyle = step3SublabelStyle,
            ButtonText = deadlinePassed ? "Open ronde" : null,
            ButtonVariant = ButtonVariant.Outlined,
            ButtonHref = $"/planner/rounds/{round.Id}/draft"
        };

        // Step 4: Planning genereren
        StepStatus step4Status;
        string step4Sublabel;
        SublabelStyle step4SublabelStyle;
        string? step4ButtonText;
        ButtonVariant step4ButtonVariant;

        if (isPublished || (hasDraft && !draftIsStale))
        {
            step4Status = StepStatus.Completed;
            step4Sublabel = "concept aangemaakt";
            step4SublabelStyle = SublabelStyle.GreenBadge;
            step4ButtonText = isPublished ? null : "Open";
            step4ButtonVariant = ButtonVariant.Outlined;
        }
        else if (draftIsStale)
        {
            step4Status = StepStatus.Attention;
            step4Sublabel = "verouderd concept";
            step4SublabelStyle = SublabelStyle.OrangeText;
            step4ButtonText = "Opnieuw genereren";
            step4ButtonVariant = ButtonVariant.Danger;
        }
        else if (step3Status == StepStatus.Completed)
        {
            step4Status = StepStatus.Current;
            step4Sublabel = "nog geen concept";
            step4SublabelStyle = SublabelStyle.GreyText;
            step4ButtonText = "Genereer";
            step4ButtonVariant = ButtonVariant.Primary;
        }
        else
        {
            step4Status = StepStatus.Future;
            step4Sublabel = "nog geen concept";
            step4SublabelStyle = SublabelStyle.GreyText;
            step4ButtonText = null;
            step4ButtonVariant = ButtonVariant.Outlined;
        }

        var step4 = new PlanStep
        {
            Number = 4,
            Title = "Planning genereren",
            Status = step4Status,
            Sublabel = step4Sublabel,
            SublabelStyle = step4SublabelStyle,
            ButtonText = step4ButtonText,
            ButtonVariant = step4ButtonVariant,
            ButtonHref = $"/planner/rounds/{round.Id}/draft"
        };

        // Step 5: Publiceren
        StepStatus step5Status;
        if (isPublished)
            step5Status = StepStatus.Completed;
        else if (step4Status == StepStatus.Completed)
            step5Status = StepStatus.Current;
        else
            step5Status = StepStatus.Future;

        var step5 = new PlanStep
        {
            Number = 5,
            Title = "Publiceren",
            Status = step5Status,
            Sublabel = isPublished ? "gepubliceerd" : $"deadline {round.PublicationDeadline:d MMM}",
            SublabelStyle = isPublished ? SublabelStyle.GreenBadge : SublabelStyle.GreyText,
            ButtonText = step5Status != StepStatus.Future ? "Open" : null,
            ButtonVariant = ButtonVariant.Outlined,
            ButtonHref = $"/planner/rounds/{round.Id}/draft"
        };

        var steps = new List<PlanStep> { step1, step2, step3, step4, step5 };
        var completedSteps = steps.Count(s => s.Status == StepStatus.Completed);
        var progress = (completedSteps * 100) / steps.Count;
        var currentStep = steps
            .Select((s, i) => (s, i + 1))
            .FirstOrDefault(x => x.s.Status != StepStatus.Completed).Item2;
        if (currentStep == 0) currentStep = steps.Count;

        return new StepPlanModel
        {
            RoundName = round.Name,
            Year = round.Year,
            Progress = progress,
            CurrentStep = currentStep,
            Steps = steps
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
