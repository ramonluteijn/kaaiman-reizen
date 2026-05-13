using Kaaiman_reizen.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services
{
    public class AccountService
    {
        private readonly MainContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountService(MainContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task CreateIdentityUserForTravelLeaderAsync(string email, string phone, string name, int id)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = phone,
                EmailConfirmed = true
            };

            // TODO Generate wachtwoord en stuur Smtpemailsender
            await _userManager.CreateAsync(user, "Kaaiman26!");
            await _userManager.AddToRoleAsync(user, "Reisleider");
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync() => await _db.Users.ToListAsync();

        public List<ApplicationUser> GetMatchingUsers(List<ApplicationUser> users, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            return users
                .Where(user =>
                    (user.UserName != null && user.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (user.Email != null && user.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}