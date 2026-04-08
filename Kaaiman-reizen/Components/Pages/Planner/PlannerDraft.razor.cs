using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;
using Kaaiman_reizen.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerDraft : ComponentBase
{
    [Inject] private IPlannerDraftService DraftService { get; set; } = default!;

    private PlannerDraftRequest? _request;
    private PlannerDraftResult? _result;
    private bool _loading = true;
    private bool _isGenerating = false;
    private List<(string Country, int Count)> _topPopular = [];
    private List<(string Country, int Count)> _leastPopular = [];
    private List<PlannerLeaderInput> _multiInterestLeaders = [];

    protected override async Task OnInitializedAsync()
    {
        _request = await DraftService.BuildRequestAsync();
        ComputeInsights();
        _loading = false;
    }

    private void ComputeInsights()
    {
        if (_request is null) return;

        var allCountries = _request.Journeys.Select(j => j.Country).Distinct();

        var popularity = allCountries
            .Select(c => (
                Country: c,
                Count: _request.Leaders.Count(l => l.PreferredDestinations.ContainsKey(c))
            ))
            .ToList();

        _topPopular    = popularity.OrderByDescending(p => p.Count).Take(3).ToList();
        _leastPopular  = popularity.OrderBy(p => p.Count).Take(3).ToList();
        _multiInterestLeaders = _request.Leaders
            .Where(l => l.PreferredDestinations.Count > 1)
            .OrderByDescending(l => l.PreferredDestinations.Count)
            .ToList();
    }

    private async Task GenerateDraftAsync()
    {
        if (_request is null) return;
        _isGenerating = true;
        _result = null;
        StateHasChanged();
        await Task.Delay(50);
        _result = await Task.Run(() => DraftService.GenerateDraft(_request));
        _isGenerating = false;
    }

    private record LeaderPlanningRow(
        int LeaderId,
        string LeaderName,
        List<(PlannerJourneyInput Journey, int? RankMatched)> AssignedJourneys,
        Dictionary<string, int> Preferences
    );

    private List<LeaderPlanningRow> BuildLeaderRows()
    {
        if (_request is null || _result is null || !_result.IsSuccess)
            return [];

        return _request.Leaders.Select(leader => new LeaderPlanningRow(
            leader.Id,
            leader.Name,
            _result.JourneyAssignments
                .Where(kvp => kvp.Value.LeaderId == leader.Id)
                .Select(kvp => (
                    Journey: _request.Journeys.First(j => j.Id == kvp.Key),
                    RankMatched: kvp.Value.RankMatched
                ))
                .ToList(),
            leader.PreferredDestinations
        )).ToList();
    }

    private IReadOnlyList<JourneyViewModel> BuildCalendarJourneys()
    {
        if (_request is null || _result is null || !_result.IsSuccess)
            return [];

        return _request.Journeys.Select(j =>
        {
            var leaders = new List<TravelLeaderViewModel>();
            if (_result.JourneyAssignments.TryGetValue(j.Id, out var assignment))
                leaders.Add(new TravelLeaderViewModel { Id = assignment.LeaderId, Name = assignment.LeaderName });

            return new JourneyViewModel
            {
                Id      = j.Id,
                Country = j.Country,
                Start   = j.Start,
                End     = j.End,
                TravelLeaders = leaders
            };
        }).ToList();
    }
}
