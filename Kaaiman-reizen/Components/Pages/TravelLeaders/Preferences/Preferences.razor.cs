using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kaaiman_reizen.Data.Enum;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Preferences;

public partial class Preferences : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private IJourneyService JourneyService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider _authProvider { get; set; } = default!;

    [Parameter]
    public int? Id { get; set; }

    private TravelLeader _model = new();
    private bool _loading = true;
    private bool _notFound = false;
    private List<PeriodModel> _preferredPeriods = new();
    private int?[] _preferredJourneyIds = new int?[3];
    private HashSet<int> _availableJourneyIds = new();
    private List<Journey> _journeys = new();

    private class PeriodModel
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

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
            var email = authState.User.Identity?.Name;

            if (!string.IsNullOrEmpty(email))
            {
                item = await LeaderService.GetTravelLeaderByEmailAsync(email);
            }
        }

        if (item == null)
        {
            _notFound = true;
            _loading = false;
            return;
        }

        _model = item;

        var allJourneys = await JourneyService.GetJourneysAsync();
        _journeys = allJourneys
            .Where(j => j.BookingStatus == BookingStatus.Bezig)
            .OrderBy(j => j.Start)
            .ToList();

        _preferredPeriods.Clear();
        if (_model.AvailabilityPeriods != null)
        {
            foreach (var p in _model.AvailabilityPeriods.OrderBy(p => p.Start))
                _preferredPeriods.Add(new PeriodModel { Start = p.Start.ToDateTime(TimeOnly.MinValue), End = p.End.ToDateTime(TimeOnly.MinValue) });
        }

        _preferredJourneyIds = new int?[3];
        _availableJourneyIds = new HashSet<int>();
        foreach (var dest in _model.PreferredDestinations)
        {
            if (dest.Rank >= 1 && dest.Rank <= 3 && dest.JourneyId.HasValue)
            {
                var idx = dest.Rank - 1;
                _preferredJourneyIds[idx] = dest.JourneyId;
            }
            else if (dest.Rank == 0 && dest.JourneyId.HasValue)
            {
                _availableJourneyIds.Add(dest.JourneyId.Value);
            }
        }

        _loading = false;
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/");
    }

    private async Task HandleValidSubmit()
    {
        _model.PreferredDestinations = new List<PreferredDestination>();

        for (int i = 0; i < 3; i++)
        {
            if (_preferredJourneyIds[i].HasValue)
                _model.PreferredDestinations.Add(new PreferredDestination
                    { Rank = i + 1, JourneyId = _preferredJourneyIds[i] });
        }

        var top3Ids = _preferredJourneyIds.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        foreach (var jid in _availableJourneyIds)
        {
            if (!top3Ids.Contains(jid))
                _model.PreferredDestinations.Add(new PreferredDestination
                    { Rank = 0, JourneyId = jid });
        }

        _model.AvailabilityPeriods = _preferredPeriods
            .Where(p => p.Start.HasValue && p.End.HasValue)
            .Select(p => new AvailabilityPeriod { Start = DateOnly.FromDateTime(p.Start!.Value), End = DateOnly.FromDateTime(p.End!.Value) })
            .ToList();

        _model.Journeys = [];
        await LeaderService.UpdateTravelLeaderAsync(_model);

        Navigation.NavigateTo("/");
    }

    private void AddPeriod()
    {
        _preferredPeriods.Add(new PeriodModel());
    }

    private void RemovePeriod(int index)
    {
        if (index >= 0 && index < _preferredPeriods.Count)
            _preferredPeriods.RemoveAt(index);
    }

    private void ToggleAvailable(int journeyId, bool isChecked)
    {
        if (isChecked) _availableJourneyIds.Add(journeyId);
        else _availableJourneyIds.Remove(journeyId);
    }
}
