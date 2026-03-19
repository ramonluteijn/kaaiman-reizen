using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kaaiman_reizen.Tests.Services
{
    public class TravelLeaderServiceTests
    {
        // Helper: create a fresh in-memory database for each test
        private MainContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<MainContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
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
    }
}