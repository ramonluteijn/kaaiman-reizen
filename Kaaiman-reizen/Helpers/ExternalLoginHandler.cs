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

            if (string.IsNullOrWhiteSpace(email))
            {
                await RedirectToLoginWithError(context, "Error: Geen e-mailadres ontvangen van de externe provider.");
                return;
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                await RedirectToLoginWithError(context, $"Error: Er is geen account gevonden dat behoort tot het e-mailadres {email}.");
                return;
            }

            var providerKey = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (providerKey is null)
            {
                await RedirectToLoginWithError(context, "Error: Inloggen mislukt. De provider heeft geen unieke identificatiecode gestuurd.");
                return;
            }

            var info = new UserLoginInfo(context.Scheme.Name, providerKey, context.Scheme.Name);
            var aspnetuserloginsTableContent = await userManager.GetLoginsAsync(user);

            if (UserLoginAlreadyExistsInTable(aspnetuserloginsTableContent, providerKey, context.Scheme.Name) is false)
                await userManager.AddLoginAsync(user, info);

            await signInManager.SignInAsync(user, isPersistent: false);

            context.HandleResponse();
            context.Response.Redirect(HOMEPAGE);
        }

        private static bool UserLoginAlreadyExistsInTable(IList<UserLoginInfo> aspnetuserloginsTableContent, string providerKey, string loginProvider)
        {
            if (aspnetuserloginsTableContent is null || providerKey is null || loginProvider is null)
                return false;

            return aspnetuserloginsTableContent.Any(login =>
                                      login.ProviderKey == providerKey &&
                                      login.LoginProvider == loginProvider);
        }

        private static async Task RedirectToLoginWithError(TicketReceivedContext context, string message)
        {
            context.HandleResponse();

            await context.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");

            context.Response.Cookies.Append("ExternalLoginError", message, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(2),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });

            context.Response.Redirect("/Account/Login");
        }
    }
}