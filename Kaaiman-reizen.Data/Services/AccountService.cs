using Kaaiman_reizen.Data.Identity;
using Microsoft.AspNetCore.Components;
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
            if (_db.TravelLeader.Any(travelLeader => travelLeader.Email == email))
            {
                throw new Exception($"Het e-mailadres {email} is al in gebruik. Gebruik a.u.b. een uniek e-mailadres dat nog niet in ons systeem voorkomt.");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = phone,
                EmailConfirmed = true
            };

            // Generate wachtwoord en stuur 
            await _userManager.CreateAsync(user, "WelkomKaaiman2026!");
            await _userManager.AddToRoleAsync(user, "Reisleider");
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync() => await _db.Users.ToListAsync() ?? new ();

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