using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kaaiman_reizen.Tests.Services
{
    public class JourneyServiceTests
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
        public async Task AddJourney_Should_Add_Journey()
        {
            var db = GetInMemoryDb();
            var service = new JourneyService(db);

            var journey = new Journey
            {
                Country = "Test Country",
                Start = new DateTime(0001, 1, 1),
                End = new DateTime(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
            };
            await service.AddJourneyAsync(journey, new List<int>());

            var allJourneys = await service.GetJourneysAsync();
            Assert.Single(allJourneys);
            Assert.Equal("Test Country", allJourneys.First().Country);
        }

        [Fact]
        public async Task UpdateJourney_Should_Update_Journey()
        {
            var db = GetInMemoryDb();
            var service = new JourneyService(db);

            var journey = new Journey
            {
                Country = "New Country",
                Start = new DateTime(0001, 1, 1),
                End = new DateTime(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
            };
            await service.AddJourneyAsync(journey, new List<int>());

            var savedJourney = (await service.GetJourneysAsync()).First();
            savedJourney.Country = "New Country";
            await service.UpdateJourneyAsync(savedJourney, new List<int>());

            var updatedJounrey = (await service.GetJourneysAsync()).First();
            Assert.Equal("New Country", updatedJounrey.Country);
        }

        [Fact]
        public async Task DeleteJourney_Should_Remove_Journey()
        {
            var db = GetInMemoryDb();
            var service = new JourneyService(db);

            var journey = new Journey
            {
                Country = "Delete Country",
                Start = new DateTime(0001, 1, 1),
                End = new DateTime(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
            }; 
            await service.AddJourneyAsync(journey, new List<int>());

            var savedJourney = (await service.GetJourneysAsync()).First();
            await service.DeleteJourneyAsync(savedJourney.Id);

            var allJourneys = await service.GetJourneysAsync();
            Assert.Empty(allJourneys);
        }

        [Fact]
        public async Task GetJourneyById_Should_Return_Correct_Journey()
        {
            var db = GetInMemoryDb();
            var service = new JourneyService(db);

            var journey = new Journey
            {
                Country = "Find Country",
                Start = new DateTime(0001, 1, 1),
                End = new DateTime(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
            }; 
            await service.AddJourneyAsync(journey, new List<int>());

            var savedJourney = (await service.GetJourneysAsync()).First();
            var foundJourney = await service.GetJourneyByIdAsync(savedJourney.Id);

            Assert.NotNull(foundJourney);
            Assert.Equal("Find Country", foundJourney.Country);
        }

        [Fact]
        public void Validate_Should_Return_Error_When_MinTrips_GreaterThan_MaxTrips()
        {
            var journey = new Journey
            {
                Country = "Invalid Country",
                Start = new DateTime(0001, 1, 2),
                End = new DateTime(0001, 1, 1),
                Busses = 1,
                Travelers = 1,
            };

            var results = journey.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(journey)).ToList();
            Assert.Single(results);
            Assert.Contains("Start datum mag niet later of gelijk zijn aan eind datum", results[0].ErrorMessage);
        }
    }
}