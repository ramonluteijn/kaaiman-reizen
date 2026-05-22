using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Services;

namespace Kaaiman_reizen.Tests.Services;

public class PlannerDraftServiceTests
{
    // the 2 nulls is just because we dont need the services.
    private readonly PlannerDraftService _service = new(null!, null!);

    [Fact]
    public void GenerateDraft_ReturnsError_WhenNoLeadersProvided()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [new PlannerJourneyInput { Id = 1, Name = "Noorwegen" }],
            Leaders = []
        };

        var result = _service.GenerateDraft(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Geen reisleiders", result.ErrorMessage);
    }

    [Fact]
    public void GenerateDraft_AssignsLeader_WhenPerfectMatch()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput
                {
                    Id = 10, Name = "Italië", RequiredLeaders = 1,
                    Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 10)
                }
            ],
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 99, Name = "Jan", MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int> { { 10, 1 } } // Beschikbaar voor reis 10
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        Assert.Single(result.JourneyAssignments[10]);
        Assert.Equal(99, result.JourneyAssignments[10].First().LeaderId);
    }

    [Fact]
    public void GenerateDraft_YieldsWarning_WhenLeaderHasNoPreferenceForJourney()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput
                {
                    Id = 1, Name = "Spanje", RequiredLeaders = 1,
                    Start = new DateOnly(2026, 8, 1), End = new DateOnly(2026, 8, 15)
                }
            ],
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Piet", MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int>() // Geen voorkeur voor reis 1
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.JourneyAssignments);
        Assert.True(result.JourneyWarnings.ContainsKey(1));
    }

    [Fact]
    public void GenerateDraft_RespectsMaxTripsConstraint()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput { Id = 1, Name = "Reis A", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 10) },
                new PlannerJourneyInput { Id = 2, Name = "Reis B", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 20), End = new DateOnly(2026, 6, 30) }
            ],
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Klaas", MaxTrips = 1,
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        var totalAssignmentsForKlaas = result.JourneyAssignments.Values
            .SelectMany(assignments => assignments)
            .Count(a => a.LeaderId == 1);
        Assert.Equal(1, totalAssignmentsForKlaas);
    }

    [Fact]
    public void GenerateDraft_ReturnsError_WhenNoJourneysProvided()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [],
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Jan", MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void GenerateDraft_Should_AssignMultipleLeaders_WhenJourneyRequiresMultiple()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput
                {
                    Id = 1, Name = "Groot Evenement", RequiredLeaders = 2,
                    Start = new DateOnly(2026, 7, 1), End = new DateOnly(2026, 7, 10)
                }
            ],
            Leaders = [
                new PlannerLeaderInput { Id = 1, Name = "Jan", MaxTrips = 5, PreferredDestinations = new Dictionary<int, int> { { 1, 1 } } },
                new PlannerLeaderInput { Id = 2, Name = "Piet", MaxTrips = 5, PreferredDestinations = new Dictionary<int, int> { { 1, 2 } } }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.JourneyAssignments.ContainsKey(1));
        Assert.Equal(2, result.JourneyAssignments[1].Count);
    }

    [Fact]
    public void GenerateDraft_ShouldNot_AssignSameLeader_ToOverlappingJourneys()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput { Id = 1, Name = "Reis A", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 15) },
                new PlannerJourneyInput { Id = 2, Name = "Reis B", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 10), End = new DateOnly(2026, 6, 20) }
            ],
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Jan", MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 1 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        var assignmentsForJan = result.JourneyAssignments.Values
            .SelectMany(a => a)
            .Where(a => a.LeaderId == 1)
            .ToList();
        Assert.True(assignmentsForJan.Count <= 1, "Een reisleider mag niet aan twee overlappende reizen worden toegewezen");
    }

    [Fact]
    public void GenerateDraft_Should_WarnAndExclude_JourneyWithInsufficientEligibleLeaders()
    {
        // Journey requires 2 leaders but only 1 leader has a preference — pre-solve should
        // detect this, issue a warning, and exclude the journey from the solver.
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput
                {
                    Id = 1, Name = "Onplanbare reis", RequiredLeaders = 2,
                    Start = new DateOnly(2026, 8, 1), End = new DateOnly(2026, 8, 10)
                }
            ],
            Leaders = [
                new PlannerLeaderInput { Id = 1, Name = "Jan", MaxTrips = 5, PreferredDestinations = new Dictionary<int, int> { { 1, 1 } } }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.JourneyWarnings.ContainsKey(1));
    }

    [Fact]
    public void GenerateDraft_Should_PreferHigherRank_WhenLeaderHasLimitedTrips()
    {
        // Leader can do 1 trip, has rank 1 for journey A and rank 3 for journey B (non-overlapping).
        // The solver should assign to journey A (rank 1 = lowest cost = highest preference).
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput { Id = 1, Name = "Eerste keuze", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 10) },
                new PlannerJourneyInput { Id = 2, Name = "Derde keuze", RequiredLeaders = 1, Start = new DateOnly(2026, 7, 1), End = new DateOnly(2026, 7, 10) }
            ],
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Jan", MaxTrips = 1,
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 3 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.JourneyAssignments.ContainsKey(1), "Reisleider moet worden toegewezen aan eerste keuze (rank 1)");
        Assert.False(result.JourneyAssignments.ContainsKey(2), "Reisleider mag niet worden toegewezen aan derde keuze als eerste keuze beschikbaar is");
    }

    [Fact]
    public void GenerateDraft_PublishingRequires_AllJourneys_ToHaveEnoughAssignments()
    {
        // The CanPublish condition in PlannerDraft.razor.cs checks that every journey
        // has at least RequiredLeaders assignments. This test verifies the solver output
        // correctly reflects a shortfall when not enough leaders are available,
        // meaning CanPublish would evaluate to false.
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput
                {
                    Id = 1, Name = "Reis met tekort", RequiredLeaders = 2,
                    Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 10)
                }
            ],
            Leaders = [
                new PlannerLeaderInput { Id = 1, Name = "Jan", MaxTrips = 5, PreferredDestinations = new Dictionary<int, int> { { 1, 1 } } }
                // Only 1 leader available, journey needs 2
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        var assignedCount = result.JourneyAssignments.TryGetValue(1, out var asgns) ? asgns.Count : 0;
        Assert.True(assignedCount < 2, "Publiceren moet geblokkeerd zijn: reis heeft minder reisleiders dan vereist");
    }
}
