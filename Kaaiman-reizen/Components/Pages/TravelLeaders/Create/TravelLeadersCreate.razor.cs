using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Create;

public partial class TravelLeadersCreate : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private TravelLeader _model = new();
    private string[] _preferred = new string[3];
    private List<PeriodModel> _periods = new();

    private class PeriodModel
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    private void AddPeriod()
    {
        _periods.Add(new PeriodModel());
    }

    private void RemovePeriod(int index)
    {
        if (index >= 0 && index < _periods.Count)
            _periods.RemoveAt(index);
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/travelleaders");
    }

    private async Task HandleValidSubmit()
    {
        // map preferred destinations
        _model.PreferredDestinations = new List<PreferredDestination>();
        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrWhiteSpace(_preferred[i]))
            {
                _model.PreferredDestinations.Add(new PreferredDestination { Rank = i + 1, Destination = _preferred[i] });
            }
        }

        // map availability periods
        _model.AvailabilityPeriods = _periods
            .Where(p => p.Start.HasValue && p.End.HasValue)
            .Select(p => new AvailabilityPeriod { Start = DateOnly.FromDateTime(p.Start!.Value), End = DateOnly.FromDateTime(p.End!.Value) })
            .ToList();

        await LeaderService.AddTravelLeaderAsync(_model);
        Navigation.NavigateTo("/travelleaders");
    }
}
