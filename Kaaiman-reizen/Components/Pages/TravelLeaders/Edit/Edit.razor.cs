using System.Linq;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Edit;

public partial class Edit : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private TravelLeader _model = new();
    private bool _loading = true;
    private bool _notFound = false;
    private List<PeriodModel> _preferredPeriods = new();

    private class PeriodModel
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _notFound = false;

        var item = await LeaderService.GetTravelLeaderByIdAsync(Id);
        if (item == null)
        {
            _notFound = true;
            _loading = false;
            return;
        }

        _model = item;

        // populate preferredPeriods
        _preferredPeriods.Clear();
        if (_model.AvailabilityPeriods != null)
        {
            foreach (var p in _model.AvailabilityPeriods.OrderBy(p => p.Start))
            {
                _preferredPeriods.Add(new PeriodModel { Start = p.Start.ToDateTime(TimeOnly.MinValue), End = p.End.ToDateTime(TimeOnly.MinValue) });
            }
        }

        _loading = false;
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/travelleaders");
    }

    private async Task HandleValidSubmit()
    {
        // map availability
        _model.AvailabilityPeriods = _preferredPeriods
            .Where(p => p.Start.HasValue && p.End.HasValue)
            .Select(p => new AvailabilityPeriod { Start = DateOnly.FromDateTime(p.Start!.Value), End = DateOnly.FromDateTime(p.End!.Value) })
            .ToList();

        await LeaderService.UpdateTravelLeaderAsync(_model);
        Navigation.NavigateTo("/travelleaders");
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
