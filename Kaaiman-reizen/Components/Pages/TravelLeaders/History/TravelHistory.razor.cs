using System.Security.Claims;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.History;

public partial class TravelHistory : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool _loading = true;
    protected string _statusMessage = string.Empty;
    protected List<Journey> _journeys = [];

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _statusMessage = string.Empty;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is not true)
        {
            _statusMessage = "Log in om je reisgeschiedenis te bekijken.";
            _loading = false;
            return;
        }

        var leaders = await LeaderService.GetTravelLeadersAsync();
        var leader = FindCurrentLeader(leaders, user);

        if (leader is null)
        {
            _statusMessage = "Geen gekoppelde reisleider gevonden voor dit account.";
            _loading = false;
            return;
        }

        _journeys = (leader.Journeys ?? [])
            .OrderByDescending(journey => journey.End)
            .ThenByDescending(journey => journey.Start)
            .ToList();

        _loading = false;
    }

    protected RenderFragment GetBookingStatusBadge(int status) => builder =>
    {
        var (badgeClass, label) = status switch
        {
            0 => ("bg-success", "Bezig"),
            1 => ("bg-warning", "Geweest"),
            2 => ("bg-danger", "Geannuleerd"),
            _ => ("bg-secondary", "Onbekend")
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"badge {badgeClass}");
        builder.AddContent(2, label);
        builder.CloseElement();
    };

    private static TravelLeader? FindCurrentLeader(IEnumerable<TravelLeader> leaders, ClaimsPrincipal user)
    {
        var leaderIdClaim = user.FindFirstValue("TravelLeaderId");
        if (int.TryParse(leaderIdClaim, out var leaderId))
        {
            var matchedById = leaders.FirstOrDefault(leader => leader.Id == leaderId);
            if (matchedById is not null)
            {
                return matchedById;
            }
        }

        return null;
    }

    private static HashSet<string> BuildCandidateNames(ClaimsPrincipal user)
    {
        var values = new List<string?>
        {
            user.Identity?.Name,
            user.FindFirstValue(ClaimTypes.Name),
            user.FindFirstValue(ClaimTypes.Email)
        };

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values.Where(static value => string.IsNullOrWhiteSpace(value) is false))
        {
            candidates.Add(Normalize(value!));

            var localPart = value!.Split('@')[0];
            candidates.Add(Normalize(localPart));
            candidates.Add(Normalize(localPart.Replace('.', ' ').Replace('-', ' ').Replace('_', ' ')));
        }

        return candidates;
    }

    private static string Normalize(string value)
    {
        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }
}