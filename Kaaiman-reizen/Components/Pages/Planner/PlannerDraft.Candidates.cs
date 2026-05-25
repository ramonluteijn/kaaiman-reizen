using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Models.ViewModels;

namespace Kaaiman_reizen.Components.Pages.Planner;

public partial class PlannerDraft
{
    private void HandleJourneyClick(JourneyViewModel journey)
    {
        _selectedJourney = journey;
        _selectedCandidates = GetCandidatesFor(journey);
        _drawerOpen = true;
    }

    private void CloseDrawer()
    {
        _drawerOpen = false;
        _selectedJourney = null;
        _selectedCandidates = [];
    }

    private void RefreshSelectedJourney()
    {
        if (_selectedJourney is null || _request is null || _result is null) return;

        var journeyInput = _request.Journeys.First(j => j.Id == _selectedJourney.Id);
        var leaders = _result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var asgns)
            ? asgns.Select(a => new TravelLeaderViewModel { Id = a.LeaderId, Name = a.LeaderName }).ToList()
            : new List<TravelLeaderViewModel>();

        _selectedJourney = new JourneyViewModel
        {
            Id = _selectedJourney.Id,
            Name = _selectedJourney.Name,
            Start = _selectedJourney.Start,
            End = _selectedJourney.End,
            RequiredLeaders = journeyInput.RequiredLeaders,
            TravelLeaders = leaders
        };
    }

    private void RemoveLeader(TravelLeaderViewModel leader)
    {
        if (_selectedJourney is null || _result is null) return;

        if (_result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var list))
        {
            list.RemoveAll(a => a.LeaderId == leader.Id);
            if (list.Count == 0)
                _result.JourneyAssignments.Remove(_selectedJourney.Id);
        }
        RefreshSelectedJourney();
        _selectedCandidates = GetCandidatesFor(_selectedJourney);
    }

    private void AssignLeader(LeaderCandidate candidate)
    {
        if (_selectedJourney is null || _result is null || _request is null) return;

        if (_result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var current)
            && current.Any(a => a.LeaderId == candidate.LeaderId))
            return;

        var journeyInput = _request.Journeys.First(j => j.Id == _selectedJourney.Id);
        int currentCount = _result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var existing)
            ? existing.Count : 0;
        if (currentCount >= journeyInput.RequiredLeaders) return;

        var leader = _request.Leaders.First(l => l.Id == candidate.LeaderId);
        int? rank = leader.PreferredDestinations.TryGetValue(_selectedJourney.Id, out int r) ? (r == 0 ? null : r) : null;

        var entry = new JourneyAssignmentResult
        {
            LeaderId = candidate.LeaderId,
            LeaderName = candidate.LeaderName,
            RankMatched = rank
        };

        if (_result.JourneyAssignments.TryGetValue(_selectedJourney.Id, out var list))
            list.Add(entry);
        else
            _result.JourneyAssignments[_selectedJourney.Id] = [entry];

        RefreshSelectedJourney();
        _selectedCandidates = GetCandidatesFor(_selectedJourney);
    }

    private List<LeaderCandidate> GetCandidatesFor(JourneyViewModel journey)
    {
        if (_request is null || _result is null) return [];

        var journeyInput = _request.Journeys.FirstOrDefault(j => j.Id == journey.Id);
        if (journeyInput is null) return [];

        return _request.Leaders
            .Select(leader => BuildLeaderCandidate(leader, journeyInput))
            .OrderBy(c => c.IsAlreadyAssigned ? 0 : 1)
            .ThenBy(c => (c.HasConflict || c.ExceedsMaxTrips || !string.IsNullOrEmpty(c.ValidationReason)) ? 1 : 0)
            .ThenBy(c => c.PreferenceRank ?? 99)
            .ToList();
    }

    private LeaderCandidate BuildLeaderCandidate(PlannerLeaderInput leader, PlannerJourneyInput journeyInput)
    {
        bool isAvailableForJourney = leader.PreferredDestinations.ContainsKey(journeyInput.Id);

        int currentCount = CountAssignments(leader.Id);
        var conflictJourney = FindConflictingJourney(leader.Id, journeyInput.Id, journeyInput);
        int? rank = leader.PreferredDestinations.TryGetValue(journeyInput.Id, out int r) ? (r == 0 ? null : r) : null;

        var assignedToThis = _result!.JourneyAssignments.TryGetValue(journeyInput.Id, out var cur)
            ? cur.Select(a => a.LeaderId).ToHashSet()
            : new HashSet<int>();

        var existingJourneys = _result.JourneyAssignments
            .Where(kvp => kvp.Value.Any(a => a.LeaderId == leader.Id) && kvp.Key != journeyInput.Id)
            .Select(kvp =>
            {
                var j = _request!.Journeys.FirstOrDefault(x => x.Id == kvp.Key);
                return j is not null ? new Kaaiman_reizen.Data.Rules.CheckRules.JourneyWindow(j.Start, j.End) : null;
            })
            .Where(x => x != null)
            .Cast<Kaaiman_reizen.Data.Rules.CheckRules.JourneyWindow>();

        Kaaiman_reizen.Data.Rules.CheckRules.CanAssignForPlanner(
            existingJourneys,
            BuildJourneyEntity(journeyInput),
            BuildLeaderEntity(leader),
            out string? validationReason,
            _ruleSettings
        );

        if (!isAvailableForJourney)
            validationReason = "Deze reisleider heeft geen voorkeur of beschikbaarheid opgegeven voor deze reis.";

        return new LeaderCandidate(
            LeaderId: leader.Id,
            LeaderName: leader.Name,
            PreferenceRank: rank,
            IsAlreadyAssigned: assignedToThis.Contains(leader.Id),
            HasConflict: conflictJourney is not null,
            ConflictJourneyName: conflictJourney is not null
                ? $"{conflictJourney.Name} ({DateDisplay.FormatDate(conflictJourney.Start)}-{DateDisplay.FormatDate(conflictJourney.End)})"
                : string.Empty,
            ExceedsMaxTrips: currentCount >= leader.MaxTrips,
            CurrentAssignments: currentCount,
            MaxTrips: leader.MaxTrips,
            ValidationReason: validationReason
        );
    }

    private int CountAssignments(int leaderId) =>
        _result!.JourneyAssignments.Values
            .SelectMany(l => l)
            .Count(a => a.LeaderId == leaderId);

    private PlannerJourneyInput? FindConflictingJourney(int leaderId, int journeyId, PlannerJourneyInput journeyInput) =>
        _result!.JourneyAssignments
            .Where(kvp => kvp.Key != journeyId && kvp.Value.Any(a => a.LeaderId == leaderId))
            .Select(kvp => _request!.Journeys.FirstOrDefault(j => j.Id == kvp.Key))
            .Where(j => j is not null && j.Start < journeyInput.End && journeyInput.Start < j.End)
            .FirstOrDefault();

    private static Journey BuildJourneyEntity(PlannerJourneyInput journeyInput) => new()
    {
        Id = journeyInput.Id,
        Name = journeyInput.Name,
        Start = journeyInput.Start,
        End = journeyInput.End
    };

    private static TravelLeader BuildLeaderEntity(PlannerLeaderInput leader) => new()
    {
        Id = leader.Id,
        Name = leader.Name,
        AmountOfTrips = leader.AmountOfTrips,
        MinTrips = leader.MinTrips,
        MaxTrips = leader.MaxTrips
    };
}
