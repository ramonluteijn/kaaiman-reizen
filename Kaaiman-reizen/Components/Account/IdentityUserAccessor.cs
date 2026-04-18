using Kaaiman_reizen.Components.Account;
using Kaaiman_reizen.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Kaaiman_reizen.Components.Account
{
    internal sealed class IdentityUserAccessor(
        UserManager<ApplicationUser> userManager,
        IdentityRedirectManager redirectManager)
    {
        public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus(
                    "Account/Login",
                    $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.",
                    context);
            }

            return user!;
        }
    }
}