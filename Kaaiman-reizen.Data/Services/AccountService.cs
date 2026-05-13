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
        private readonly IEmailDispatcher _emailDispatcher;

        public AccountService(MainContext db, UserManager<ApplicationUser> userManager, IEmailDispatcher emailDispatcher)
        {
            _db = db;
            _userManager = userManager;
            _emailDispatcher = emailDispatcher;
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

            var password = GeneratePassword();

            await _userManager.CreateAsync(user, password);
            await _userManager.AddToRoleAsync(user, "Reisleider");
            await _userManager.AddClaimAsync(user, new Claim("TravelLeaderId", id.ToString()));

            await _emailDispatcher.SendEmailAsync(
                email,
                "Welkom bij Kaaiman Reizen",
                $"Beste {name},<br><br>Je account is aangemaakt.<br>Je tijdelijk wachtwoord is: <strong>{password}</strong><br><br>Na je eerste keer inloggen kun je je wachtwoord wijzigen door op je e-mailadres te klikken linksonder in het menu."
            );
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

        private string GeneratePassword()
        {
            var options = _userManager.Options.Password;

            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}";

            var random = new Random();
            var password = new List<char>
            {
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                special[random.Next(special.Length)]
            };

            const string allChars = upper + lower + digits + special;
            int remainingLength = Math.Max(options.RequiredLength, 12) - password.Count;

            for (int i = 0; i < remainingLength; i++)
                password.Add(allChars[random.Next(allChars.Length)]);

            return new string(password.OrderBy(_ => random.Next()).ToArray());
        }
    }
}