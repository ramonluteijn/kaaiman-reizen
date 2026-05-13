using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            await _userManager.AddClaimAsync(user, new Claim("TravelLeaderId", id.ToString()));
        }

        public async Task DeleteAccountByEmailAsync(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(user => user.Email == email);

            if (user is not null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync() => await _db.Users.ToListAsync() ?? new();

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

        public async Task UpdateAccountAsync(TravelLeader model)
        {
            var guid = _db.UserClaims.First(claim => claim.ClaimType == "TravelLeaderId" && claim.ClaimValue == model.Id.ToString()).UserId;
            var account = await _userManager.FindByIdAsync(guid);

            if (account is null)
                throw new Exception("Account kon niet gevonden worden.");

            account.UserName = model.Email;
            account.Email = model.Email;
            account.PhoneNumber = model.PhoneNumber;

            await _userManager.UpdateAsync(account);
        }
    }
}