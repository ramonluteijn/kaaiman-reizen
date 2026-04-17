using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Preferences;

public partial class Preferences : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int? Id { get; set; }

    private TravelLeader _model = new();
    private bool _loading = true;
    private bool _notFound = false;
    private List<PeriodModel> _preferredPeriods = new();
    private string[] _preferred = new string[3];

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
        if (Id.HasValue && Id.Value > 0)
        {
            item = await LeaderService.GetTravelLeaderByIdAsync(Id.Value);
        }
        else
        {
            var all = await LeaderService.GetTravelLeadersAsync();
            item = all.FirstOrDefault();
            if (item != null)
                Id = item.Id;
        }

        if (item == null)
        {
            _notFound = true;
            _loading = false;
            return;
        }

        _model = item;

        _preferredPeriods.Clear();
        if (_model.AvailabilityPeriods != null)
        {
            foreach (var p in _model.AvailabilityPeriods.OrderBy(p => p.Start))
                _preferredPeriods.Add(new PeriodModel { Start = p.Start.ToDateTime(TimeOnly.MinValue), End = p.End.ToDateTime(TimeOnly.MinValue) });
        }

        _preferred = new string[3];
        foreach (var dest in _model.PreferredDestinations)
        {
            var idx = dest.Rank - 1;
            if (idx >= 0 && idx < 3)
                _preferred[idx] = dest.Destination;
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
            if (!string.IsNullOrWhiteSpace(_preferred[i]))
                _model.PreferredDestinations.Add(new PreferredDestination { Rank = i + 1, Destination = _preferred[i] });
        }

        _model.AvailabilityPeriods = _preferredPeriods
            .Where(p => p.Start.HasValue && p.End.HasValue)
            .Select(p => new AvailabilityPeriod { Start = DateOnly.FromDateTime(p.Start!.Value), End = DateOnly.FromDateTime(p.End!.Value) })
            .ToList();

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
}
