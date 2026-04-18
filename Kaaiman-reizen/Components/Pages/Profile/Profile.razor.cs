using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kaaiman_reizen.Data.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Kaaiman_reizen.Components.Pages.Profile;

public partial class Profile : ComponentBase
{
    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    public UserManager<ApplicationUser> UserManager { get; set; } = default!;

    private ChangePasswordModel passwordModel = new();
    private string statusMessage = "";
    private string statusColor = "success";

    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Huidig wachtwoord is verplicht")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Nieuw wachtwoord is verplicht")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Bevestig nieuw wachtwoord is verplicht")]
        [Compare("NewPassword", ErrorMessage = "Wachtwoorden komen niet overeen")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }

    private async Task ChangePassword()
    {
        statusMessage = "";

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var appUser = await UserManager.GetUserAsync(user);
            if (appUser != null)
            {
                var result = await UserManager.ChangePasswordAsync(appUser, passwordModel.CurrentPassword, passwordModel.NewPassword);
                if (result.Succeeded)
                {
                    statusMessage = "Wachtwoord succesvol gewijzigd.";
                    statusColor = "success";
                    passwordModel = new ChangePasswordModel(); // Reset form
                }
                else
                {
                    statusMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                    statusColor = "danger";
                }
            }
        }
    }

    protected string GetUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity?.Name ?? "Onbekend";
    }

    protected IEnumerable<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
    }
}
