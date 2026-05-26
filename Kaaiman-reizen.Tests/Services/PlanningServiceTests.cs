using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kaaiman_reizen.Tests.Services
{
    public class PlanningServiceTests
    {
        private class DummyEmailDispatcher : IEmailDispatcher
        {
            public List<string> SentEmails { get; } = new();

            public Task SendEmailAsync(string email, string subject, string message)
            {
                SentEmails.Add(email);
                return Task.CompletedTask;
            }

            public Task SendEmailToUsersAsync(List<string> emailAddresses, string subject, string message)
            {
                SentEmails.AddRange(emailAddresses);
                return Task.CompletedTask;
            }
        }

        private class DummyServiceProvider : IServiceProvider, IServiceScopeFactory, IServiceScope
        {
            private readonly DummyEmailDispatcher _dispatcher;

            public DummyServiceProvider(DummyEmailDispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }

            public IServiceProvider ServiceProvider => this;

            public IServiceScope CreateScope() => this;

            public void Dispose() { }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceScopeFactory))
                    return this;

                if (serviceType == typeof(IEmailDispatcher))
                    return _dispatcher;

                return null;
            }
        }

        private MainContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<MainContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MainContext(options);
        }

        [Fact]
        public async Task SavePlanningAsync_WhenPublished_ShouldCreateNotificationsForAllUsers()
        {
            // Arrange
            var db = GetInMemoryDb();

            // Setup some users
            var user1 = new ApplicationUser { Id = "user1", Email = "test1@example.com" };
            var user2 = new ApplicationUser { Id = "user2", Email = "test2@example.com" };
            var user3 = new ApplicationUser { Id = "user3", Email = null }; // no email

            db.Users.AddRange(user1, user2, user3);
            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);

            var planningService = new PlanningService(db, serviceProvider);

            // Act
            var assignments = new Dictionary<int, IReadOnlyCollection<int>>();
            var result = await planningService.SavePlanningAsync(2026, "Definitieve planning", true, assignments);

            // Give the fire-and-forget task a moment to execute (since it runs on a thread pool)
            await Task.Delay(100);

            // Assert
            var notifications = await db.Notifications.ToListAsync();

            Assert.Equal(3, notifications.Count);
            Assert.Contains(notifications, n => n.ApplicationUserId == "user1" && n.Message.Contains("gepubliceerd"));
            Assert.Contains(notifications, n => n.ApplicationUserId == "user2" && !n.IsRead);
            Assert.Contains(notifications, n => n.ApplicationUserId == "user3");

            // Verify emails were sent to the users with emails
            Assert.Equal(2, dispatcher.SentEmails.Count);
            Assert.Contains("test1@example.com", dispatcher.SentEmails);
            Assert.Contains("test2@example.com", dispatcher.SentEmails);
        }

        [Fact]
        public async Task SavePlanningAsync_ShouldStoreCreatedAtAsUtc_InDatabase()
        {
            // Arrange
            var db = GetInMemoryDb();
            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            // Act
            await planningService.SavePlanningAsync(2026, "Draft planning", false, new Dictionary<int, IReadOnlyCollection<int>>());

            // Assert
            var planningVersion = await db.PlanningVersions.SingleAsync();
            Assert.Equal(DateTimeKind.Utc, planningVersion.CreatedAt.Kind);
        }
    }
}
