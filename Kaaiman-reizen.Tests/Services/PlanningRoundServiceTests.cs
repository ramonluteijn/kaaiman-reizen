using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Tests.Services;

public class PlanningRoundServiceTests
{
    private static MainContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<MainContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MainContext(options);
    }

    private static PlanningRound CreateRound() => new()
    {
        Name = "Ronde 2026",
        Year = 2026,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        PreferenceDeadline = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        PublicationDeadline = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static TravelLeader CreateLeader(string name = "Test Reisleider") => new()
    {
        Name = name,
        Email = "test@example.com",
        PhoneNumber = "0612345678",
        AmountOfTrips = 5,
        MinTrips = 0,
        MaxTrips = 3,
        IsActive = true
    };

    private static Journey CreateJourney(string name = "Testreis") => new()
    {
        Name = name,
        Start = new DateOnly(2026, 6, 1),
        End = new DateOnly(2026, 6, 10),
        Busses = 1,
        Travelers = 20,
        BookingStatus = BookingStatus.Huidig
    };

    // ── MarkUnavailableAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task MarkUnavailableAsync_SetsStatusToUnavailable()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        await db.SaveChangesAsync();

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Pending
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        var updated = await db.PlanningRoundParticipations.FindAsync(participation.Id);
        Assert.Equal(ParticipationStatus.Unavailable, updated!.Status);
    }

    [Fact]
    public async Task MarkUnavailableAsync_ClearsExistingPreferences()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        var journey = CreateJourney();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted,
            Preferences =
            [
                new PlanningRoundPreference { JourneyId = journey.Id, Rank = 1 },
                new PlanningRoundPreference { JourneyId = journey.Id, Rank = 0 }
            ]
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.PlanningRoundPreferences.CountAsync());

        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        Assert.Equal(0, await db.PlanningRoundPreferences.CountAsync());
    }

    [Fact]
    public async Task MarkUnavailableAsync_SetsSubmittedAt()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        await db.SaveChangesAsync();

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Pending,
            SubmittedAt = null
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        var before = DateTime.UtcNow;
        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        var updated = await db.PlanningRoundParticipations.FindAsync(participation.Id);
        Assert.NotNull(updated!.SubmittedAt);
        Assert.True(updated.SubmittedAt >= before);
    }

    [Fact]
    public async Task MarkUnavailableAsync_RemovesDraftAssignmentsForLeader()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        var journey = CreateJourney();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var draftVersion = new PlanningVersion
        {
            Name = "Concept",
            PlanningRoundId = round.Id,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };
        db.PlanningVersions.Add(draftVersion);
        await db.SaveChangesAsync();

        db.PlanningAssignments.Add(new PlanningAssignment
        {
            PlanningVersionId = draftVersion.Id,
            TravelLeaderId = leader.Id,
            JourneyId = journey.Id
        });

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.PlanningAssignments.CountAsync());

        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        Assert.Equal(0, await db.PlanningAssignments.CountAsync());
    }

    [Fact]
    public async Task MarkUnavailableAsync_PreservesPublishedAssignments()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        var journey = CreateJourney();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var publishedVersion = new PlanningVersion
        {
            Name = "Definitief",
            PlanningRoundId = round.Id,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };
        db.PlanningVersions.Add(publishedVersion);
        await db.SaveChangesAsync();

        db.PlanningAssignments.Add(new PlanningAssignment
        {
            PlanningVersionId = publishedVersion.Id,
            TravelLeaderId = leader.Id,
            JourneyId = journey.Id
        });

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        Assert.Equal(1, await db.PlanningAssignments.CountAsync());
    }

    [Fact]
    public async Task MarkUnavailableAsync_DoesNotAffectOtherLeaderDraftAssignments()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader("Reisleider A");
        var otherLeader = CreateLeader("Reisleider B");
        otherLeader.Email = "other@example.com";
        var round = CreateRound();
        var journey = CreateJourney();
        db.TravelLeader.AddRange(leader, otherLeader);
        db.PlanningRounds.Add(round);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var draftVersion = new PlanningVersion
        {
            Name = "Concept",
            PlanningRoundId = round.Id,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };
        db.PlanningVersions.Add(draftVersion);
        await db.SaveChangesAsync();

        db.PlanningAssignments.AddRange(
            new PlanningAssignment { PlanningVersionId = draftVersion.Id, TravelLeaderId = leader.Id, JourneyId = journey.Id },
            new PlanningAssignment { PlanningVersionId = draftVersion.Id, TravelLeaderId = otherLeader.Id, JourneyId = journey.Id }
        );

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        var remaining = await db.PlanningAssignments.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(otherLeader.Id, remaining[0].TravelLeaderId);
    }

    [Fact]
    public async Task MarkUnavailableAsync_DoesNotAffectDraftAssignmentsFromOtherRounds()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        var otherRound = new PlanningRound
        {
            Name = "Ronde 2027",
            Year = 2027,
            StartDate = new DateOnly(2027, 1, 1),
            EndDate = new DateOnly(2027, 12, 31),
            PreferenceDeadline = new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PublicationDeadline = new DateTime(2027, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var journey = CreateJourney();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.AddRange(round, otherRound);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var otherDraftVersion = new PlanningVersion
        {
            Name = "Concept 2027",
            PlanningRoundId = otherRound.Id,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };
        db.PlanningVersions.Add(otherDraftVersion);
        await db.SaveChangesAsync();

        db.PlanningAssignments.Add(new PlanningAssignment
        {
            PlanningVersionId = otherDraftVersion.Id,
            TravelLeaderId = leader.Id,
            JourneyId = journey.Id
        });

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        await new PlanningRoundService(db).MarkUnavailableAsync(participation.Id);

        Assert.Equal(1, await db.PlanningAssignments.CountAsync());
    }

    [Fact]
    public async Task MarkUnavailableAsync_DoesNothingWhenParticipationNotFound()
    {
        var db = GetInMemoryDb();

        await new PlanningRoundService(db).MarkUnavailableAsync(participationId: 999);

        Assert.Equal(0, await db.PlanningRoundParticipations.CountAsync());
    }

    // ── Heraanmelden na afmelden ─────────────────────────────────────────────────

    [Fact]
    public async Task SavePreferencesAsync_AfterMarkUnavailable_SetsStatusBackToSubmitted()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        var journey = CreateJourney();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted,
            Preferences = [new PlanningRoundPreference { JourneyId = journey.Id, Rank = 1 }]
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        var service = new PlanningRoundService(db);

        await service.MarkUnavailableAsync(participation.Id);
        Assert.Equal(ParticipationStatus.Unavailable, (await db.PlanningRoundParticipations.FindAsync(participation.Id))!.Status);

        await service.SavePreferencesAsync(participation.Id, [(journey.Id, 1)], null, null, string.Empty);

        var updated = await db.PlanningRoundParticipations
            .Include(p => p.Preferences)
            .FirstAsync(p => p.Id == participation.Id);

        Assert.Equal(ParticipationStatus.Submitted, updated.Status);
        Assert.Single(updated.Preferences);
        Assert.Equal(journey.Id, updated.Preferences[0].JourneyId);
        Assert.Equal(1, updated.Preferences[0].Rank);
    }

    [Fact]
    public async Task SavePreferencesAsync_AfterMarkUnavailable_HasNoPreferencesFromBefore()
    {
        var db = GetInMemoryDb();
        var leader = CreateLeader();
        var round = CreateRound();
        var journey = CreateJourney();
        db.TravelLeader.Add(leader);
        db.PlanningRounds.Add(round);
        db.Journey.Add(journey);
        await db.SaveChangesAsync();

        var participation = new PlanningRoundParticipation
        {
            PlanningRoundId = round.Id,
            TravelLeaderId = leader.Id,
            Status = ParticipationStatus.Submitted,
            Preferences =
            [
                new PlanningRoundPreference { JourneyId = journey.Id, Rank = 1 },
                new PlanningRoundPreference { JourneyId = journey.Id, Rank = 2 }
            ]
        };
        db.PlanningRoundParticipations.Add(participation);
        await db.SaveChangesAsync();

        var service = new PlanningRoundService(db);
        await service.MarkUnavailableAsync(participation.Id);

        // Re-submit with only one preference
        await service.SavePreferencesAsync(participation.Id, [(journey.Id, 0)], null, null, string.Empty);

        Assert.Equal(1, await db.PlanningRoundPreferences.CountAsync());
    }

}
