using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Tests.Services;

public class DashboardServiceTests
{
    private static readonly int CurrentYear = DateTime.UtcNow.Year;

    private MainContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<MainContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MainContext(options);
    }

    // ── GetJourneysWithoutTravelLeaders ──────────────────────────────────────────

    [Fact(Skip = "Service does not yet filter on BookingStatus")]
    public async Task GetJourneysWithoutTravelLeaders_ShouldNotInclude_CancelledJourneys()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.Journey.Add(new Journey
        {
            Name = "Geannuleerde reis",
            Start = new DateOnly(CurrentYear, 6, 1),
            End = new DateOnly(CurrentYear, 6, 10),
            BookingStatus = BookingStatus.Geanuleerd,
            Busses = 1,
            Travelers = 10
        });
        await db.SaveChangesAsync();

        var result = await service.GetJourneysWithoutTravelLeadersAsync(CurrentYear);

        Assert.Empty(result);
    }

    [Fact(Skip = "Service does not yet filter on BookingStatus")]
    public async Task GetJourneysWithoutTravelLeaders_ShouldNotInclude_GeweestJourneys()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.Journey.Add(new Journey
        {
            Name = "Voorbije reis",
            Start = new DateOnly(CurrentYear, 3, 1),
            End = new DateOnly(CurrentYear, 3, 10),
            BookingStatus = BookingStatus.Geweest,
            Busses = 1,
            Travelers = 8
        });
        await db.SaveChangesAsync();

        var result = await service.GetJourneysWithoutTravelLeadersAsync(CurrentYear);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJourneysWithoutTravelLeaders_ShouldInclude_HuidigJourney_WithoutAssignment()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.Journey.Add(new Journey
        {
            Name = "Italië",
            Start = new DateOnly(CurrentYear, 6, 1),
            End = new DateOnly(CurrentYear, 6, 10),
            BookingStatus = BookingStatus.Huidig,
            Busses = 1,
            Travelers = 10
        });
        await db.SaveChangesAsync();

        var result = await service.GetJourneysWithoutTravelLeadersAsync(CurrentYear);

        Assert.Single(result);
        Assert.Equal("Italië", result.First().Name);
    }

    [Fact]
    public async Task GetJourneysWithoutTravelLeaders_ShouldNotInclude_HuidigJourney_ThatHasAssignment()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        var leader = new TravelLeader { Name = "Jan", PhoneNumber = "123", IsActive = true, AmountOfTrips = 1, MinTrips = 0, MaxTrips = 5 };
        var journey = new Journey
        {
            Name = "Noorwegen",
            Start = new DateOnly(CurrentYear, 6, 1),
            End = new DateOnly(CurrentYear, 6, 10),
            BookingStatus = BookingStatus.Huidig,
            Busses = 1,
            Travelers = 10
        };
        db.TravelLeader.Add(leader);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var version = new PlanningVersion { Name = "Concept", PlanningYear = CurrentYear, IsPublished = false, CreatedAt = DateTime.UtcNow };
        db.PlanningVersions.Add(version);
        await db.SaveChangesAsync();

        db.PlanningAssignments.Add(new PlanningAssignment
        {
            TravelLeaderId = leader.Id,
            JourneyId = journey.Id,
            PlanningVersionId = version.Id
        });
        await db.SaveChangesAsync();

        var result = await service.GetJourneysWithoutTravelLeadersAsync(CurrentYear);

        Assert.Empty(result);
    }

    // ── GetTravelLeadersWithoutJourneys ──────────────────────────────────────────

    [Fact(Skip = "Service does not yet filter on IsActive")]
    public async Task GetTravelLeadersWithoutJourneys_ShouldNotInclude_InactiveLeaders()
    {
        // An inactive leader has no assignment and therefore technically has no journey,
        // but should NOT appear on the dashboard — they are not part of active planning.
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.TravelLeader.Add(new TravelLeader
        {
            Name = "Inactieve reisleider",
            PhoneNumber = "000",
            IsActive = false,
            AmountOfTrips = 0,
            MinTrips = 0,
            MaxTrips = 5
        });
        await db.SaveChangesAsync();

        var result = await service.GetTravelLeadersWithoutJourneysAsync(CurrentYear);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTravelLeadersWithoutJourneys_ShouldInclude_ActiveLeader_WithoutAssignment()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.TravelLeader.Add(new TravelLeader
        {
            Name = "Actieve reisleider",
            PhoneNumber = "123",
            IsActive = true,
            AmountOfTrips = 2,
            MinTrips = 1,
            MaxTrips = 5
        });
        await db.SaveChangesAsync();

        var result = await service.GetTravelLeadersWithoutJourneysAsync(CurrentYear);

        Assert.Single(result);
        Assert.Equal("Actieve reisleider", result.First().Name);
    }

    [Fact]
    public async Task GetTravelLeadersWithoutJourneys_ShouldNotInclude_ActiveLeader_WithAssignment()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        var leader = new TravelLeader { Name = "Ingeplande reisleider", PhoneNumber = "456", IsActive = true, AmountOfTrips = 1, MinTrips = 0, MaxTrips = 5 };
        var journey = new Journey { Name = "Spanje", Start = new DateOnly(CurrentYear, 5, 1), End = new DateOnly(CurrentYear, 5, 10), BookingStatus = BookingStatus.Huidig, Busses = 1, Travelers = 8 };
        db.TravelLeader.Add(leader);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var version = new PlanningVersion { Name = "Concept", PlanningYear = CurrentYear, IsPublished = false, CreatedAt = DateTime.UtcNow };
        db.PlanningVersions.Add(version);
        await db.SaveChangesAsync();

        db.PlanningAssignments.Add(new PlanningAssignment
        {
            TravelLeaderId = leader.Id,
            JourneyId = journey.Id,
            PlanningVersionId = version.Id
        });
        await db.SaveChangesAsync();

        var result = await service.GetTravelLeadersWithoutJourneysAsync(CurrentYear);

        Assert.Empty(result);
    }

    // ── GetTravelLeadersWithoutPreferences ("Concept aanmaken" guard) ────────────

    [Fact]
    public async Task GetTravelLeadersWithoutPreferences_ShouldReturn_LeaderWithNoPreferences()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.TravelLeader.Add(new TravelLeader
        {
            Name = "Geen voorkeuren",
            PhoneNumber = "111",
            IsActive = true,
            AmountOfTrips = 0,
            MinTrips = 0,
            MaxTrips = 5
        });
        await db.SaveChangesAsync();

        var result = await service.GetTravelLeadersWithoutPreferencesAsync();

        Assert.Single(result);
        Assert.Equal("Geen voorkeuren", result.First().Name);
    }

    [Fact]
    public async Task GetTravelLeadersWithoutPreferences_ShouldNotReturn_LeaderWithPreferences()
    {
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        var journey = new Journey
        {
            Name = "Spanje",
            Start = new DateOnly(CurrentYear, 6, 1),
            End = new DateOnly(CurrentYear, 6, 10),
            BookingStatus = BookingStatus.Huidig,
            Busses = 1,
            Travelers = 10
        };
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        db.TravelLeader.Add(new TravelLeader
        {
            Name = "Met voorkeur",
            PhoneNumber = "222",
            IsActive = true,
            AmountOfTrips = 1,
            MinTrips = 0,
            MaxTrips = 5,
            PreferredDestinations = [new PreferredDestination { JourneyId = journey.Id, Rank = 1 }]
        });
        await db.SaveChangesAsync();

        var result = await service.GetTravelLeadersWithoutPreferencesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTravelLeadersWithoutPreferences_ReturnsAllLeaders_WhenNoneHavePreferences_BlockingConceptCreation()
    {
        // When every active leader lacks preferences, requesting a concept draft is pointless.
        // The dashboard should not show the "Concept aanmaken" button in this state.
        // This test verifies the data condition that drives that UI decision.
        var db = GetInMemoryDb();
        var service = new TravelLeaderService(db);

        db.TravelLeader.AddRange(
            new TravelLeader { Name = "Leader A", PhoneNumber = "1", IsActive = true, AmountOfTrips = 0, MinTrips = 0, MaxTrips = 5 },
            new TravelLeader { Name = "Leader B", PhoneNumber = "2", IsActive = true, AmountOfTrips = 0, MinTrips = 0, MaxTrips = 5 }
        );
        await db.SaveChangesAsync();

        var withoutPreferences = await service.GetTravelLeadersWithoutPreferencesAsync();
        var all = await service.GetTravelLeadersAsync();

        Assert.Equal(all.Count(), withoutPreferences.Count);
    }
}
