using Kaaiman_reizen.Models.Planner;
using Kaaiman_reizen.Data.Rules;
using Kaaiman_reizen.Services;

namespace Kaaiman_reizen.Tests.Services;

public class PlannerDraftServiceTests
{
    // the 2 nulls is just because we dont need the services.
    private readonly PlannerDraftService _service = new(null!, null!, null!);

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
            RuleSettings = new CheckRules.RuleSettings(
                NoOverlapActive: true,
                MinimumGapDaysActive: true,
                MinimumGapDays: 3,
                RequiredExperienceActive: false,
                RequiredExperience: 3,
                MinMaxJourneysActive: true,
                PreferencesEnabled: true,
                PreferenceWeight: 5,
                NoOverlapWeight: 200,
                MinimumGapWeight: 100,
                RequiredExperienceWeight: 100),
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Klaas", MaxTrips = 1,
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } } // Beschikbaar voor beide reizen
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
    public void GenerateDraft_PreferencesDisabled_StillRespectsAvailability()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput
                {
                    Id = 3, Name = "Reis C", RequiredLeaders = 1,
                    Start = new DateOnly(2026, 7, 1), End = new DateOnly(2026, 7, 10)
                }
            ],
            RuleSettings = new CheckRules.RuleSettings(
                NoOverlapActive: true,
                MinimumGapDaysActive: true,
                MinimumGapDays: 3,
                RequiredExperienceActive: false,
                RequiredExperience: 3,
                MinMaxJourneysActive: true,
                PreferencesEnabled: false,
                PreferenceWeight: 5,
                NoOverlapWeight: 200,
                MinimumGapWeight: 100,
                RequiredExperienceWeight: 100),
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 10, Name = "Onbeschikbaar", MaxTrips = 5,
                    PreferredDestinations = []
                },
                new PlannerLeaderInput
                {
                    Id = 11, Name = "Beschikbaar", MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int> { { 3, 0 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.JourneyAssignments.TryGetValue(3, out var assignments));
        Assert.Single(assignments);
        Assert.Equal(11, assignments[0].LeaderId);
    }

    [Fact]
    public void GenerateDraft_RespectMinimumGapDaysConstraint_WhenActive()
    {
        // Test that when MinimumGapDaysActive is true, a leader cannot be assigned to two journeys
        // that don't have enough gap between them
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput { Id = 1, Name = "Noorwegen", RequiredLeaders = 1, Start = new DateOnly(2026, 10, 4), End = new DateOnly(2026, 10, 14) },
                new PlannerJourneyInput { Id = 2, Name = "IJsland", RequiredLeaders = 1, Start = new DateOnly(2026, 10, 18), End = new DateOnly(2026, 10, 28) }
                // Gap = 4 days, minimum gap requirement = 30 days
            ],
            RuleSettings = new CheckRules.RuleSettings(
                NoOverlapActive: true,
                MinimumGapDaysActive: true,  // Hard constraint on minimum gap
                MinimumGapDays: 30,
                RequiredExperienceActive: false,
                RequiredExperience: 0,
                MinMaxJourneysActive: true,
                PreferencesEnabled: true,
                PreferenceWeight: 5,
                NoOverlapWeight: 200,
                MinimumGapWeight: 100,
                RequiredExperienceWeight: 100),
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "Ramon", MaxTrips = 5,
                    // Marked as available for both journeys
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 1 } }
                },
                new PlannerLeaderInput
                {
                    Id = 2, Name = "Alternative", MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int> { { 2, 1 } }  // Available for second journey only
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        // Ramon should NOT be assigned to both journeys due to insufficient gap
        var ramonAssignments = result.JourneyAssignments.Values
            .SelectMany(assignments => assignments)
            .Count(a => a.LeaderId == 1);
        Assert.True(ramonAssignments <= 1, "Ramon should not be assigned to both journeys with gap < 30 days");
        
        // The second journey should be assigned to the Alternative leader
        Assert.True(result.JourneyAssignments.TryGetValue(2, out var secondJourneyAssignments));
        Assert.Contains(secondJourneyAssignments, a => a.LeaderId == 2);
    }

    [Fact]
    public void GenerateDraft_RespectsMinTripsConstraint_WhenActive()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput { Id = 1, Name = "Reis A", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 10) },
                new PlannerJourneyInput { Id = 2, Name = "Reis B", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 20), End = new DateOnly(2026, 6, 30) }
            ],
            RuleSettings = new CheckRules.RuleSettings(
                NoOverlapActive: true,
                MinimumGapDaysActive: false,
                MinimumGapDays: 0,
                RequiredExperienceActive: false,
                RequiredExperience: 0,
                MinMaxJourneysActive: true,
                PreferencesEnabled: true,
                PreferenceWeight: 1,
                NoOverlapWeight: 0,
                MinimumGapWeight: 0,
                RequiredExperienceWeight: 0),
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "MinTripsLeader", MinTrips = 2, MaxTrips = 5,
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 1 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        var totalAssignments = result.JourneyAssignments.Values
            .SelectMany(assignments => assignments)
            .Count(a => a.LeaderId == 1);
        Assert.Equal(2, totalAssignments);
    }

    [Fact]
    public void GenerateDraft_IgnoresMinMaxConstraints_WhenDisabled()
    {
        var request = new PlannerDraftRequest
        {
            Journeys = [
                new PlannerJourneyInput { Id = 1, Name = "Reis A", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 10) },
                new PlannerJourneyInput { Id = 2, Name = "Reis B", RequiredLeaders = 1, Start = new DateOnly(2026, 6, 20), End = new DateOnly(2026, 6, 30) }
            ],
            RuleSettings = new CheckRules.RuleSettings(
                NoOverlapActive: true,
                MinimumGapDaysActive: false,
                MinimumGapDays: 0,
                RequiredExperienceActive: false,
                RequiredExperience: 0,
                MinMaxJourneysActive: false, // Disabled
                PreferencesEnabled: true,
                PreferenceWeight: 1,
                NoOverlapWeight: 0,
                MinimumGapWeight: 0,
                RequiredExperienceWeight: 0),
            Leaders = [
                new PlannerLeaderInput
                {
                    Id = 1, Name = "UnlimitedLeader", MinTrips = 10, MaxTrips = 0, // Should be ignored
                    PreferredDestinations = new Dictionary<int, int> { { 1, 1 }, { 2, 1 } }
                }
            ]
        };

        var result = _service.GenerateDraft(request);

        Assert.True(result.IsSuccess);
        var totalAssignments = result.JourneyAssignments.Values
            .SelectMany(assignments => assignments)
            .Count(a => a.LeaderId == 1);
        // Should assign to both because Min/Max are ignored
        Assert.Equal(2, totalAssignments);
    }
}
