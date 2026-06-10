using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Services;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Tests.Services
{
    public class TravelLeaderServiceTests
    {
        private class MockJobScheduler : IJobScheduler
        {
            public Task ScheduleJobsForJourney(int journeyId, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RescheduleJobsForJourney(int journeyId, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveJobsForJourney(int journeyid, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private IJobScheduler GetMockJobScheduler()
        {
            return new MockJobScheduler();
        }
        // Helper: create a fresh in-memory database for each test
        private MainContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<MainContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new MainContext(options);
        }

        [Fact]
        public async Task AddTravelLeader_Should_Add_Leader()
        {
            var db = GetInMemoryDb();
            var service = new TravelLeaderService(db);

            var leader = new TravelLeader
            {
                Name = "Test Leader",
                PhoneNumber = "123456",
                AmountOfTrips = 5,
                MinTrips = 1,
                MaxTrips = 10,
                IsActive = true
            };

            await service.AddTravelLeaderAsync(leader);

            var allLeaders = await service.GetTravelLeadersAsync();
            Assert.Single(allLeaders);
            Assert.Equal("Test Leader", allLeaders.First().Name);
        }

        [Fact]
        public async Task UpdateTravelLeader_Should_Update_Leader()
        {
            var db = GetInMemoryDb();
            var service = new TravelLeaderService(db);

            var leader = new TravelLeader { Name = "Old Name", PhoneNumber = "111", AmountOfTrips = 2, MinTrips = 1, MaxTrips = 5 };
            await service.AddTravelLeaderAsync(leader);

            var savedLeader = (await service.GetTravelLeadersAsync()).First();
            savedLeader.Name = "New Name";
            await service.UpdateTravelLeaderAsync(savedLeader);

            var updatedLeader = (await service.GetTravelLeadersAsync()).First();
            Assert.Equal("New Name", updatedLeader.Name);
        }

        [Fact]
        public async Task DeleteTravelLeader_Should_Remove_Leader()
        {
            var db = GetInMemoryDb();
            var service = new TravelLeaderService(db);

            var leader = new TravelLeader { Name = "Delete Me", PhoneNumber = "000", AmountOfTrips = 1, MinTrips = 0, MaxTrips = 5 };
            await service.AddTravelLeaderAsync(leader);

            var savedLeader = (await service.GetTravelLeadersAsync()).First();
            await service.DeleteTravelLeaderAsync(savedLeader.Id);

            var allLeaders = await service.GetTravelLeadersAsync();
            Assert.Empty(allLeaders);
        }

        [Fact]
        public async Task ToggleActive_Should_Flip_IsActive()
        {
            var db = GetInMemoryDb();
            var service = new TravelLeaderService(db);

            var leader = new TravelLeader { Name = "Active Leader", PhoneNumber = "123", AmountOfTrips = 3, MinTrips = 1, MaxTrips = 5, IsActive = true };
            await service.AddTravelLeaderAsync(leader);

            var savedLeader = (await service.GetTravelLeadersAsync()).First();
            savedLeader.IsActive = !savedLeader.IsActive;
            await service.UpdateTravelLeaderAsync(savedLeader);

            var updatedLeader = (await service.GetTravelLeadersAsync()).First();
            Assert.False(updatedLeader.IsActive);
        }

        [Fact]
        public async Task GetTravelLeaderById_Should_Return_Correct_Leader()
        {
            var db = GetInMemoryDb();
            var service = new TravelLeaderService(db);

            var leader = new TravelLeader { Name = "Find Me", PhoneNumber = "999", AmountOfTrips = 2, MinTrips = 1, MaxTrips = 5 };
            await service.AddTravelLeaderAsync(leader);

            var savedLeader = (await service.GetTravelLeadersAsync()).First();
            var foundLeader = await service.GetTravelLeaderByIdAsync(savedLeader.Id);

            Assert.NotNull(foundLeader);
            Assert.Equal("Find Me", foundLeader.Name);
        }

        [Fact]
        public void Validate_Should_Return_Error_When_MinTrips_GreaterThan_MaxTrips()
        {
            var leader = new TravelLeader
            {
                Name = "Invalid Leader",
                PhoneNumber = "000",
                MinTrips = 10,
                MaxTrips = 5
            };

            var results = leader.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(leader)).ToList();
            Assert.Single(results);
            Assert.Contains("Minimaal aantal reizen mag niet groter zijn dan maximaal aantal reizen", results[0].ErrorMessage);
        }

        [Fact]
        public async Task GetJourneysOfTravelLeader_Should_Return_Correct_Journeys()
        {
            var db = GetInMemoryDb();
            var leaderService = new TravelLeaderService(db);
            var journeyService = new JourneyService(db, null, GetMockJobScheduler());

            var leader1 = new TravelLeader { Id = 1, Name = "Leader 1", PhoneNumber = "123", AmountOfTrips = 3, MinTrips = 1, MaxTrips = 5, IsActive = true };
            var leader2 = new TravelLeader { Id = 2, Name = "Leader 2", PhoneNumber = "123", AmountOfTrips = 3, MinTrips = 1, MaxTrips = 5, IsActive = true };
            await leaderService.AddTravelLeaderAsync(leader1);
            await leaderService.AddTravelLeaderAsync(leader2);

            var journey1 = new Journey { Name = "Journey 1", Start = new DateOnly(0001, 1, 1), End = new DateOnly(0001, 1, 2), Busses = 1, Travelers = 1, BookingStatus = BookingStatus.Geweest };
            var journey2 = new Journey { Name = "Journey 2", Start = new DateOnly(0001, 1, 1), End = new DateOnly(0001, 1, 2), Busses = 1, Travelers = 1, BookingStatus = BookingStatus.Geweest };
            await journeyService.AddJourneyAsync(journey1, [1]);
            await journeyService.AddJourneyAsync(journey2, [2]);

            var journeys1 = await leaderService.GetJourneysOfTravelLeaderAsync(leader1);
            var journeys2 = await leaderService.GetJourneysOfTravelLeaderAsync(leader2);

            Assert.Contains(journeys1, j => j.Id == journey1.Id);
            Assert.DoesNotContain(journeys1, j => j.Id == journey2.Id);
            Assert.Contains(journeys2, j => j.Id == journey2.Id);
            Assert.DoesNotContain(journeys2, j => j.Id == journey1.Id);
        }

        [Fact]
        public async Task ArchiveAndResetPreferredDestinations_Should_Copy_To_History_And_Clear()
        {
            var db = GetInMemoryDb();
            var service = new TravelLeaderService(db);

            var leader = new TravelLeader
            {
                Name = "Leader",
                PhoneNumber = "123",
                AmountOfTrips = 1,
                MinTrips = 0,
                MaxTrips = 5,
                IsActive = true
            };
            db.TravelLeader.Add(leader);
            await db.SaveChangesAsync();

            db.PreferredDestinations.AddRange(
                new PreferredDestination { TravelLeaderId = leader.Id, JourneyId = 10, Rank = 1 },
                new PreferredDestination { TravelLeaderId = leader.Id, JourneyId = 11, Rank = 0 });
            await db.SaveChangesAsync();

            var archivedCount = await service.ArchiveAndResetPreferredDestinationsAsync(42);

            Assert.Equal(2, archivedCount);
            Assert.Empty(db.PreferredDestinations);

            var history = await db.TravelLeaderAvailabilityHistories.ToListAsync();
            Assert.Equal(2, history.Count);
            Assert.All(history, entry => Assert.Equal(42, entry.PlanningVersionId));
            Assert.Contains(history, entry => entry.JourneyId == 10 && entry.Rank == 1);
            Assert.Contains(history, entry => entry.JourneyId == 11 && entry.Rank == 0);
        }
    }
}