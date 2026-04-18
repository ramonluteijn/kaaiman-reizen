using Kaaiman_reizen.Data.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Kaaiman_reizen.Helpers
{
    public static class ExternalLoginHandler
    {
        private const string REISLEIDER = "Reisleider";
        private const string HOMEPAGE = "/";

        public static async Task HandleExternalLogin(TicketReceivedContext context)
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);

            if (email?.ToString() is not null)
            {
                var user = await userManager.FindByEmailAsync(email);

                if (user is null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user);
                }

                if (!await userManager.IsInRoleAsync(user, REISLEIDER))
                    await userManager.AddToRoleAsync(user, REISLEIDER);

                var providerKey = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (providerKey is not null)
                {
                    var info = new UserLoginInfo(context.Scheme.Name, providerKey, context.Scheme.Name);
                    var aspnetuserloginsTableContent = await userManager.GetLoginsAsync(user);

                    if (UserLoginAlreadyExistsInTable(aspnetuserloginsTableContent, providerKey, context.Scheme.Name) is false)
                        await userManager.AddLoginAsync(user, info);
                }

                await signInManager.SignInAsync(user, isPersistent: false);

                context.HandleResponse();

                context.Response.Redirect(HOMEPAGE);
            }
        }

        private static bool UserLoginAlreadyExistsInTable(IList<UserLoginInfo> aspnetuserloginsTableContent, string providerKey, string loginProvider)
        {
            if (aspnetuserloginsTableContent is null || providerKey is null || loginProvider is null)
                return false;

            return aspnetuserloginsTableContent.Any(login => 
                                      login.ProviderKey == providerKey && 
                                      login.LoginProvider == loginProvider);
        }
    }
}