using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Exports;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using QuestPDF.Fluent;
using System.Diagnostics;
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
    [Inject] private IJSRuntime JS { get; set; }

    private bool _loading = true;
    private bool _isPlanner;
    private bool _isReisleider;
    private List<PlanningVersion> _drafts = [];
    private List<Journey> _publishedJourneys = [];
    private int _selectedYear = DateTime.UtcNow.Year;
    private bool _publishedPlanning;
    private List<Journey> _plannedJourneysWithTravelLeaders;

    private List<Notification> _notifications = [];
    private string _currentUserId = string.Empty;

    private List<TravelLeader> _travelLeadersWithoutPreferences = [];
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
            var drafts = await PlanningService.GetDraftsAsync(_selectedYear);
            _drafts = drafts.ToList();

            _travelLeadersWithoutPreferences = await TravelLeaderService.GetTravelLeadersWithoutPreferencesAsync();
            _travelLeadersWithoutJourneys = await TravelLeaderService.GetTravelLeadersWithoutJourneysAsync(_selectedYear);
            _travelLeadersWithNotes = await TravelLeaderService.GetTravelLeadersWithNotesAsync();
            _journeysWithoutTravelLeaders = await TravelLeaderService.GetJourneysWithoutTravelLeadersAsync(_selectedYear);
            _travelLeadersWithOverlappingJourneys = await TravelLeaderService.GetTravelLeadersWithOverlappingJourneys();

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

    private async Task MarkNotificationAsRead(Notification notification)
    {
        await NotificationService.MarkAsReadAsync(notification.Id, _currentUserId);
        _notifications.Remove(notification);
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
        Debug.WriteLine("test");

        try
        {
            var journeys = await PlanningService.GetAllJourneysWithTravelLeadersFromLatestPublishedPlanning();
            var document = new PlanningDocument(journeys);
            byte[] pdfBytes = document.GeneratePdf();

            Console.WriteLine($"PDF gegenereerd: {pdfBytes.Length} bytes"); // Zie je dit in je output?

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
