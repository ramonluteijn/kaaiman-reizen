using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kaaiman_reizen.Tests.Services
{
    public class PlanningServiceTests
    {
        private class DummyEmailDispatcher : IEmailDispatcher
        {
            public List<string> SentEmails { get; } = new();
            public List<(string Email, string Subject)> SentMessages { get; } = new();

            public Task SendEmailAsync(string email, string subject, string message)
            {
                SentEmails.Add(email);
                SentMessages.Add((email, subject));
                return Task.CompletedTask;
            }

            public Task SendEmailToUsersAsync(List<string> emailAddresses, string subject, string message)
            {
                SentEmails.AddRange(emailAddresses);
                foreach (var email in emailAddresses)
                    SentMessages.Add((email, subject));
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

        private static PlanningRound CreateRound(int year = 2026) => new()
        {
            Name = "Ronde 2026",
            Year = year,
            StartDate = new DateOnly(year, 1, 1),
            EndDate = new DateOnly(year, 12, 31),
            PreferenceDeadline = new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PublicationDeadline = new DateTime(year, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public async Task SavePlanningForRoundAsync_WhenPublished_ShouldCreateNotificationsForAllUsers()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);

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
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Definitieve planning", true, assignments);

            // Give the fire-and-forget task a moment to execute (since it runs on a thread pool)
            await Task.Delay(100);

            // Assert
            var notifications = await db.Notifications.ToListAsync();

            Assert.Equal(3, notifications.Count);
            Assert.Contains(notifications, n => n.ApplicationUserId == "user1" && n.Message.Contains("gepubliceerd"));
            Assert.Contains(notifications, n => n.ApplicationUserId == "user2" && !n.IsRead);
            Assert.Contains(notifications, n => n.ApplicationUserId == "user3");

            Assert.Equal(2, dispatcher.SentEmails.Count);
            Assert.Contains("test1@example.com", dispatcher.SentEmails);
            Assert.Contains("test2@example.com", dispatcher.SentEmails);
        }

        [Fact]
        public async Task SavePlanningForRoundAsync_WhenPublished_ShouldBeVisibleInArchive()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);

            db.Users.AddRange(
                new ApplicationUser { Id = "user1", Email = "test1@example.com" },
                new ApplicationUser { Id = "user2", Email = "test2@example.com" });
            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            // Act
            var assignments = new Dictionary<int, IReadOnlyCollection<int>>();
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Definitieve planning", true, assignments);
            await Task.Delay(100);

            // Assert
            var archivedPlans = await planningService.GetPublishedPlansAsync();
            Assert.Single(archivedPlans);
        }

        [Fact]
        public async Task SavePlanningForRoundAsync_WhenPublished_ShouldUnpublishPreviousVersionForSameRound()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);
            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            var assignments = new Dictionary<int, IReadOnlyCollection<int>>();

            // Act: publish twice for the same round
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Planning v1", true, assignments);
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Planning v2", true, assignments);
            await Task.Delay(100);

            // Assert: only the latest is published
            var allVersions = await db.PlanningVersions.ToListAsync();
            Assert.Equal(2, allVersions.Count);
            Assert.Single(allVersions, v => v.IsPublished);
            Assert.Equal("Planning v2", allVersions.Single(v => v.IsPublished).Name);
        }

        [Fact]
        public async Task SavePlanningForRoundAsync_Draft_ShouldStoreCreatedAtAsUtc()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);
            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            // Act
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Draft planning", false, new Dictionary<int, IReadOnlyCollection<int>>());

            // Assert
            var planningVersion = await db.PlanningVersions.SingleAsync();
            Assert.Equal(DateTimeKind.Utc, planningVersion.CreatedAt.Kind);
        }

        [Fact]
        public async Task SavePlanningForRoundAsync_Draft_ShouldUpsertExistingDraft()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);
            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            var assignments = new Dictionary<int, IReadOnlyCollection<int>>();

            // Act: save draft twice
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Draft v1", false, assignments);
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Draft v2", false, assignments);

            // Assert: still only one draft, with updated name
            var drafts = await db.PlanningVersions.Where(v => !v.IsPublished).ToListAsync();
            Assert.Single(drafts);
            Assert.Equal("Draft v2", drafts[0].Name);
        }

        [Fact]
        public async Task SavePlanningForRoundAsync_WhenRepublishedWithChanges_ShouldNotifyOnlyAffectedTravelLeadersAboutChange()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);

            var role = new IdentityRole { Id = "role-reisleider", Name = "Reisleider", NormalizedName = "REISLEIDER" };
            db.Roles.Add(role);

            var reisleiderUser = new ApplicationUser { Id = "user-reisleider", Email = "reisleider@example.com" };
            var changedToUser = new ApplicationUser { Id = "user-changed-to", Email = "changedto@example.com" };
            var unaffectedLeaderUser = new ApplicationUser { Id = "user-unaffected", Email = "unaffected@example.com" };
            var plannerUser = new ApplicationUser { Id = "user-planner", Email = "planner@example.com" };

            db.Users.AddRange(reisleiderUser, changedToUser, unaffectedLeaderUser, plannerUser);
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = reisleiderUser.Id, RoleId = role.Id });
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = changedToUser.Id, RoleId = role.Id });
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = unaffectedLeaderUser.Id, RoleId = role.Id });

            db.TravelLeader.AddRange(
                new TravelLeader
                {
                    Id = 100,
                    Name = "Leader A",
                    Email = "reisleider@example.com",
                    PhoneNumber = "0612345678",
                    AmountOfTrips = 1,
                    MinTrips = 0,
                    MaxTrips = 2
                },
                new TravelLeader
                {
                    Id = 101,
                    Name = "Leader B",
                    Email = "changedto@example.com",
                    PhoneNumber = "0612345679",
                    AmountOfTrips = 1,
                    MinTrips = 0,
                    MaxTrips = 2
                },
                new TravelLeader
                {
                    Id = 102,
                    Name = "Leader C",
                    Email = "unaffected@example.com",
                    PhoneNumber = "0612345680",
                    AmountOfTrips = 1,
                    MinTrips = 0,
                    MaxTrips = 2
                });

            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            var initialAssignments = new Dictionary<int, IReadOnlyCollection<int>>
            {
                [1] = [100]
            };

            var changedAssignments = new Dictionary<int, IReadOnlyCollection<int>>
            {
                [1] = [101]
            };

            // Act
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Planning v1", true, initialAssignments);
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Planning v2", true, changedAssignments);
            await Task.Delay(100);

            // Assert: dashboard notification only for the travel leader
            var changedNotifications = await db.Notifications
                .Where(n => n.Message.Contains("is gewijzigd"))
                .ToListAsync();

            Assert.Contains(changedNotifications, n => n.ApplicationUserId == reisleiderUser.Id);
            Assert.Contains(changedNotifications, n => n.ApplicationUserId == changedToUser.Id);
            Assert.DoesNotContain(changedNotifications, n => n.ApplicationUserId == unaffectedLeaderUser.Id);

            // Assert: change email sent only to affected travel leaders, not unaffected leader/planner
            var changeEmails = dispatcher.SentMessages
                .Where(m => m.Subject.Contains("gewijzigd"))
                .ToList();
            Assert.Contains(changeEmails, m => m.Email == "reisleider@example.com");
            Assert.Contains(changeEmails, m => m.Email == "changedto@example.com");
            Assert.DoesNotContain(changeEmails, m => m.Email == "unaffected@example.com");
            Assert.DoesNotContain(changeEmails, m => m.Email == "planner@example.com");
        }

        [Fact]
        public async Task SavePlanningForRoundAsync_WhenRepublishedWithoutChanges_ShouldNotCreateChangeNotification()
        {
            // Arrange
            var db = GetInMemoryDb();

            var round = CreateRound();
            db.PlanningRounds.Add(round);

            var role = new IdentityRole { Id = "role-reisleider", Name = "Reisleider", NormalizedName = "REISLEIDER" };
            db.Roles.Add(role);

            var reisleiderUser = new ApplicationUser { Id = "user-reisleider", Email = "reisleider@example.com" };
            db.Users.Add(reisleiderUser);
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = reisleiderUser.Id, RoleId = role.Id });
            await db.SaveChangesAsync();

            var dispatcher = new DummyEmailDispatcher();
            var serviceProvider = new DummyServiceProvider(dispatcher);
            var planningService = new PlanningService(db, serviceProvider);

            var assignments = new Dictionary<int, IReadOnlyCollection<int>>
            {
                [1] = [100]
            };

            // Act
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Planning v1", true, assignments);
            await planningService.SavePlanningForRoundAsync(round.Id, round.Year, "Planning v2", true, assignments);

            // Assert
            var changedNotifications = await db.Notifications
                .Where(n => n.Message.Contains("is gewijzigd"))
                .ToListAsync();

            Assert.Empty(changedNotifications);
        }
    }
}
