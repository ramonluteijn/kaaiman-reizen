using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kaaiman_reizen.Tests.Services
{
    public class JourneyNotificationServiceTests
    {
        private class DummyEmailDispatcher : IEmailDispatcher
        {
            public List<(string email, string subject, string message)> SentEmails { get; } = new();

            public Task SendEmailAsync(string email, string subject, string message)
            {
                SentEmails.Add((email, subject, message));
                return Task.CompletedTask;
            }

            public Task SendEmailToUsersAsync(List<string> emailAddresses, string subject, string message)
            {
                foreach (var email in emailAddresses)
                    SentEmails.Add((email, subject, message));
                return Task.CompletedTask;
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
        public async Task SendJourneyRemindersAsync_WhenJourneyStartsIn7Days_SendsEmailAndNotification()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(7);

            // Create ApplicationUser
            var applicationUser = new ApplicationUser { Id = "user-1", Email = "john@example.com" };

            // Create travel leader
            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                PhoneNumber = "123456789",
                AmountOfTrips = 5,
                MinTrips = 1,
                MaxTrips = 10
            };

            // Create journey
            var journey = new Journey
            {
                Id = 1,
                Name = "Paris Trip",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(3),
                Busses = 2,
                Travelers = 30,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(db, emailDispatcher, notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert
            // Check email was sent
            Assert.Single(emailDispatcher.SentEmails);
            var (email, subject, message) = emailDispatcher.SentEmails.First();
            Assert.Equal("john@example.com", email);
            Assert.Contains("Paris Trip", subject);
            Assert.Contains("7 dag", message);

            // Check dashboard notification was created
            var notifications = await db.Notifications.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal("user-1", notifications.First().ApplicationUserId);
            Assert.Contains("Paris Trip", notifications.First().Message);

            // Check notification history was created
            var history = await db.JourneyNotificationHistory
                .Where(h => h.JourneyId == 1 && h.DaysBeforeStart == 7)
                .ToListAsync();
            Assert.Single(history);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WhenJourneyStartsIn3Days_SendsEmailAndNotification()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(3);

            // Create ApplicationUser
            var applicationUser = new ApplicationUser { Id = "user-2", Email = "jane@example.com" };

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Jane Smith",
                Email = "jane@example.com",
                PhoneNumber = "987654321",
                AmountOfTrips = 3,
                MinTrips = 1,
                MaxTrips = 5
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Amsterdam Trip",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(2),
                Busses = 1,
                Travelers = 20,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(db, emailDispatcher, notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert
            Assert.Single(emailDispatcher.SentEmails);
            var (email, subject, message) = emailDispatcher.SentEmails.First();
            Assert.Equal("jane@example.com", email);
            Assert.Contains("3 dag", message);
            Assert.Contains("Amsterdam Trip", subject);

            // Check dashboard notification
            var notifications = await db.Notifications.ToListAsync();
            Assert.Single(notifications);
            Assert.Equal("user-2", notifications.First().ApplicationUserId);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WhenNoDuplicates_PreventsMultipleSends()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(7);

            var applicationUser = new ApplicationUser
            {
                Id = "1",
                Email = "test@example.com"
            };

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Test Trip",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(1),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);

            db.JourneyNotificationHistory.Add(new JourneyNotificationHistory
            {
                JourneyId = 1,
                ApplicationUserId = "1",
                DaysBeforeStart = 7,
                SentAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService =
                new JourneyNotificationService(db, emailDispatcher, notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert
            Assert.Empty(emailDispatcher.SentEmails);

            Assert.Single(await db.JourneyNotificationHistory.ToListAsync());
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WithMultipleTravelLeaders_SendsToAll()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(7);

            // Create ApplicationUsers
            var user1 = new ApplicationUser { Id = "user-1", Email = "leader1@example.com" };
            var user2 = new ApplicationUser { Id = "user-2", Email = "leader2@example.com" };

            var leader1 = new TravelLeader
            {
                Id = 1,
                Name = "Leader 1",
                Email = "leader1@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var leader2 = new TravelLeader
            {
                Id = 2,
                Name = "Leader 2",
                Email = "leader2@example.com",
                PhoneNumber = "222222222",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Group Trip",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(1),
                Busses = 2,
                Travelers = 40,
                RequiredLeaders = 2,
                TravelLeaders = new List<TravelLeader> { leader1, leader2 }
            };

            db.Users.AddRange(user1, user2);
            db.TravelLeader.AddRange(leader1, leader2);
            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(db, emailDispatcher, notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert - emails sent to both leaders
            Assert.Equal(2, emailDispatcher.SentEmails.Count);
            Assert.Contains(emailDispatcher.SentEmails, e => e.email == "leader1@example.com");
            Assert.Contains(emailDispatcher.SentEmails, e => e.email == "leader2@example.com");

            // Check notifications created for both users
            var notifications = await db.Notifications.ToListAsync();
            Assert.Equal(2, notifications.Count);
            Assert.Contains(notifications, n => n.ApplicationUserId == "user-1");
            Assert.Contains(notifications, n => n.ApplicationUserId == "user-2");

            // Check notification history for both leaders
            var historyCount = await db.JourneyNotificationHistory
                .Where(h => h.JourneyId == 1 && h.DaysBeforeStart == 7)
                .CountAsync();
            Assert.Equal(2, historyCount);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WithoutReminder_DoesNotSend()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(5); // Not 3 or 7 days

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Test Trip",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(1),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(db, emailDispatcher, notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert - no email sent
            Assert.Empty(emailDispatcher.SentEmails);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WithBothReminders_Sends7And3DayNotifications()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var applicationUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@example.com"
            };

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey7Days = new Journey
            {
                Id = 1,
                Name = "7 Days Trip",
                Start = today.AddDays(7),
                End = today.AddDays(8),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            var journey3Days = new Journey
            {
                Id = 2,
                Name = "3 Days Trip",
                Start = today.AddDays(3),
                End = today.AddDays(4),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.AddRange(journey7Days, journey3Days);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(
                db,
                emailDispatcher,
                notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert
            Assert.Equal(2, emailDispatcher.SentEmails.Count);
            Assert.Contains(emailDispatcher.SentEmails, e => e.message.Contains("7 dag"));
            Assert.Contains(emailDispatcher.SentEmails, e => e.message.Contains("3 dag"));

            var history = await db.JourneyNotificationHistory.ToListAsync();

            Assert.Equal(2, history.Count);
            Assert.Contains(history, h => h.JourneyId == 1 && h.DaysBeforeStart == 7);
            Assert.Contains(history, h => h.JourneyId == 2 && h.DaysBeforeStart == 3);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WhenJourneyHasNoLeaders_DoesNothing()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(7);

            var journey = new Journey
            {
                Id = 1,
                Name = "Empty Leaders Trip",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(1),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader>() // No leaders
            };

            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(db, emailDispatcher, notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert - no emails sent
            Assert.Empty(emailDispatcher.SentEmails);
            var historyCount = await db.JourneyNotificationHistory.CountAsync();
            Assert.Equal(0, historyCount);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_EmailMessageFormat_ContainsCorrectInfo()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var journeyStartDate = today.AddDays(7);

            var applicationUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@example.com"
            };

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Barcelona Expedition",
                Start = journeyStartDate,
                End = journeyStartDate.AddDays(3),
                Busses = 2,
                Travelers = 30,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(
                db,
                emailDispatcher,
                notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert
            Assert.Single(emailDispatcher.SentEmails);

            var (email, subject, message) = emailDispatcher.SentEmails.First();

            Assert.Equal("test@example.com", email);
            Assert.Equal("Herinnering: Uw reis naar Barcelona Expedition", subject);
            Assert.Contains("Barcelona Expedition", message);
            Assert.Contains("7 dag", message);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_SeparateLead3And7DayNotifications()
        {
            // Arrange
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var applicationUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@example.com"
            };

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Test Trip",
                Start = today.AddDays(7),
                End = today.AddDays(8),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(
                db,
                emailDispatcher,
                notificationService);

            // First run (7-day reminder)
            await journeyNotificationService.SendJourneyRemindersAsync();

            Assert.Single(emailDispatcher.SentEmails);

            var history7Days = await db.JourneyNotificationHistory
                .CountAsync(h => h.DaysBeforeStart == 7);

            Assert.Equal(1, history7Days);

            emailDispatcher.SentEmails.Clear();

            var journey3Days = new Journey
            {
                Id = 2,
                Name = "3 Days Trip",
                Start = today.AddDays(3),
                End = today.AddDays(4),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            db.Journey.Add(journey3Days);
            await db.SaveChangesAsync();

            // Second run (3-day reminder)
            await journeyNotificationService.SendJourneyRemindersAsync();

            Assert.Single(emailDispatcher.SentEmails);
            Assert.Contains("3 dag", emailDispatcher.SentEmails.First().message);

            var history3Days = await db.JourneyNotificationHistory
                .CountAsync(h => h.DaysBeforeStart == 3);

            Assert.Equal(1, history3Days);

            var totalHistory = await db.JourneyNotificationHistory.CountAsync();
            Assert.Equal(2, totalHistory);
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_WhenDisabledRuleExists_DoesNotSend()
        {
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Disabled Notifications Trip",
                Start = today.AddDays(7),
                End = today.AddDays(8),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            var disabledRule = new Rule
            {
                Id = 101,
                Key = "JourneyReminderEnabled",
                Description = "Toggle journey reminders",
                Value = "false",
                IsActive = true,
                Weight = 1
            };

            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            db.Rule.Add(disabledRule);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(db, emailDispatcher, notificationService);

            await journeyNotificationService.SendJourneyRemindersAsync();

            Assert.Empty(emailDispatcher.SentEmails);
            Assert.Empty(await db.JourneyNotificationHistory.ToListAsync());
        }

        [Fact]
        public async Task SendJourneyRemindersAsync_UsesConfiguredReminderDays()
        {
            var db = GetInMemoryDb();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var applicationUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@example.com"
            };

            var travelLeader = new TravelLeader
            {
                Id = 1,
                Name = "Test Leader",
                Email = "test@example.com",
                PhoneNumber = "111111111",
                AmountOfTrips = 1,
                MinTrips = 1,
                MaxTrips = 1
            };

            var journey = new Journey
            {
                Id = 1,
                Name = "Custom Day Trip",
                Start = today.AddDays(10),
                End = today.AddDays(11),
                Busses = 1,
                Travelers = 10,
                RequiredLeaders = 1,
                TravelLeaders = new List<TravelLeader> { travelLeader }
            };

            var reminderDaysRule = new Rule
            {
                Id = 102,
                Key = "JourneyReminderDays",
                Description = "Days before start",
                Value = "10,5",
                IsActive = true,
                Weight = 1
            };

            db.Users.Add(applicationUser);
            db.TravelLeader.Add(travelLeader);
            db.Journey.Add(journey);
            db.Rule.Add(reminderDaysRule);
            await db.SaveChangesAsync();

            var emailDispatcher = new DummyEmailDispatcher();
            var notificationService = new NotificationService(db);
            var journeyNotificationService = new JourneyNotificationService(
                db,
                emailDispatcher,
                notificationService);

            // Act
            await journeyNotificationService.SendJourneyRemindersAsync();

            // Assert
            Assert.Single(emailDispatcher.SentEmails);
            Assert.Contains("10 dag", emailDispatcher.SentEmails.First().message);

            var history = await db.JourneyNotificationHistory.ToListAsync();

            Assert.Single(history);
            Assert.Equal(10, history.First().DaysBeforeStart);
            Assert.Equal("user-1", history.First().ApplicationUserId);
        }
    }
}
