using Kaaiman_reizen.Data.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services
{
    public class AccountService
    {
        private readonly MainContext _db;

        public AccountService(MainContext db)
        {
            _db = db;
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