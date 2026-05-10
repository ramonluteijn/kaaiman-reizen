using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Tests.Services
{
    public class NotificationServiceTests
    {
        private MainContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<MainContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MainContext(options);
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_ShouldReturnOnlyUnreadForUser()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = new NotificationService(db);

            var user1 = new ApplicationUser { Id = "user1" };
            var user2 = new ApplicationUser { Id = "user2" };
            db.Users.AddRange(user1, user2);

            db.Notifications.AddRange(
                new Notification { Id = 1, ApplicationUserId = "user1", IsRead = false, Message = "msg1", CreatedAt = DateTime.UtcNow },
                new Notification { Id = 2, ApplicationUserId = "user1", IsRead = true, Message = "msg2", CreatedAt = DateTime.UtcNow },
                new Notification { Id = 3, ApplicationUserId = "user2", IsRead = false, Message = "msg3", CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            // Act
            var notifications = await service.GetUnreadNotificationsAsync("user1");

            // Assert
            Assert.Single(notifications);
            Assert.Equal("msg1", notifications.First().Message);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldUpdateIsRead()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = new NotificationService(db);

            var user1 = new ApplicationUser { Id = "user1" };
            db.Users.Add(user1);

            var notification = new Notification { Id = 1, ApplicationUserId = "user1", IsRead = false, Message = "msg1", CreatedAt = DateTime.UtcNow };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            // Act
            await service.MarkAsReadAsync(1, "user1");

            // Assert
            var dbNotification = await db.Notifications.FindAsync(1);
            Assert.True(dbNotification.IsRead);
        }

        [Fact]
        public async Task MarkAsReadAsync_WrongUser_ShouldNotUpdate()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = new NotificationService(db);

            var user1 = new ApplicationUser { Id = "user1" };
            db.Users.Add(user1);

            var notification = new Notification { Id = 1, ApplicationUserId = "user1", IsRead = false, Message = "msg1", CreatedAt = DateTime.UtcNow };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            // Act
            await service.MarkAsReadAsync(1, "otherUser");

            // Assert
            var dbNotification = await db.Notifications.FindAsync(1);
            Assert.False(dbNotification.IsRead);
        }
    }
}
