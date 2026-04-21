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
                    AvailabilityPeriods = [ (new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 1)) ], // Ruim beschikbaar
                    PreferredDestinations = new Dictionary<string, int>()
                }
            ]
        };
        var result = _service.GenerateDraft(request);
        
        Assert.True(result.IsSuccess);
        Assert.Single(result.JourneyAssignments[10]); // Er moet precies 1 toewijzing zijn
        Assert.Equal(99, result.JourneyAssignments[10].First().LeaderId); // Jan is toegewezen
    }

    [Fact]
    public void GenerateDraft_YieldsWarning_WhenLeaderNotAvailableForDates()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput 
                { 
                    Id = 1, Name = "Spanje", RequiredLeaders = 1, 
                    Start = new DateOnly(2026, 8, 1), End = new DateOnly(2026, 8, 15) // Reis in Augustus
                }
            ],
            Leaders = [
                new PlannerLeaderInput 
                {
                    Id = 1, Name = "Piet", MaxTrips = 5,
                    AvailabilityPeriods = [ (new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)) ] // Beschikbaar in Juni
                }
            ]
        };
        
        var result = _service.GenerateDraft(request);
        
        Assert.True(result.IsSuccess); 
        Assert.Empty(result.JourneyAssignments); // There is no that can be planned in
        Assert.True(result.JourneyWarnings.ContainsKey(1)); // There is a warning containing the problem.
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
                    AvailabilityPeriods = [ (new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)) ]
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
}