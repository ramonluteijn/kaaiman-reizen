using Kaaiman_reizen.Data.Entities;
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
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private Microsoft.AspNetCore.Identity.UserManager<Kaaiman_reizen.Data.Identity.ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private INotificationService NotificationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _loading = true;
    private bool _isPlanner;
    private bool _isReisleider;
    private List<Journey> _publishedJourneys = [];
    private int _selectedYear = DateTime.UtcNow.Year;
    private bool _publishedPlanning;
    private bool _publishedPlanningIsComplete;
    private List<Journey> _plannedJourneysWithTravelLeaders = [];
    private int _userTimezoneOffsetMinutes;
    private bool _userTimezoneOffsetLoaded;

    private List<Notification> _notifications = [];
    private string _currentUserId = string.Empty;

    private List<TravelLeader> _travelLeadersWithoutJourneys = [];
    private List<TravelLeader> _travelLeadersWithNotes = [];
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
            _travelLeadersWithoutJourneys = await TravelLeaderService.GetTravelLeadersWithoutJourneysAsync(_selectedYear);
            _travelLeadersWithNotes = await TravelLeaderService.GetTravelLeadersWithNotesAsync();
            _journeysWithoutTravelLeaders = await TravelLeaderService.GetJourneysWithoutTravelLeadersAsync(_selectedYear);
            _travelLeadersWithOverlappingJourneys = await TravelLeaderService.GetTravelLeadersWithOverlappingJourneys();

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

    private async Task MarkNotificationAsRead(Notification notification)
    {
        await NotificationService.MarkAsReadAsync(notification.Id, _currentUserId);
        _notifications.Remove(notification);
    }

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

    private string FormatCreatedAt(DateTime createdAt)
    {
        var userLocal = DateDisplay.ToUserLocal(createdAt, _userTimezoneOffsetMinutes);
        return DateDisplay.FormatDateTime(userLocal);
    }

    private static string FormatTravelLeaders(Journey journey)
    {
        var leaders = journey.TravelLeaders ?? [];
        if (leaders.Count == 0)
        {
            return "-";
        }

        return string.Join("; ", leaders.OrderBy(leader => leader.Name).Select(leader => leader.Name));
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
