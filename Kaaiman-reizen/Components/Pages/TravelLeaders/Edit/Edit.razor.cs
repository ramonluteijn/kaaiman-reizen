using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Edit;

public partial class Edit : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private AccountService _accountService { get; set; } = default!;

    [Inject]
    private MainContext _db { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> _userManager { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private TravelLeader _model = new();
    private bool _loading = true;
    private bool _notFound = false;
    private string? _errorMessage;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _notFound = false;

        var item = await LeaderService.GetTravelLeaderByIdAsync(Id);
        if (item == null)
        {
            _notFound = true;
            _loading = false;
            return;
        }

        _model = item;
        _loading = false;
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/travelleaders");
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            var userClaim = await _db.UserClaims.FirstOrDefaultAsync(claim =>
                claim.ClaimType == "TravelLeaderId" && claim.ClaimValue == _model.Id.ToString());

            var currentAccount = await _userManager.FindByIdAsync(userClaim.UserId);

            var emailExists = await _db.TravelLeader
                .AnyAsync(tl => tl.Id != _model.Id && tl.Email == _model.Email);

            var emailExistsInIdentity = await _userManager.FindByEmailAsync(_model.Email);

            if (emailExists || (emailExistsInIdentity is not null && emailExistsInIdentity.Email != currentAccount.Email))
                throw new Exception($"Het e-mailadres {_model.Email} is al in gebruik.");

            var phoneExists = await _db.TravelLeader
                .AnyAsync(tl => tl.Id != _model.Id && tl.PhoneNumber == _model.PhoneNumber);

            var phoneExistsInIdentity = await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == _model.PhoneNumber && u.Id != currentAccount.Id);

            if (phoneExists || phoneExistsInIdentity)
                throw new Exception($"Het telefoonnummer {_model.PhoneNumber} is al in gebruik.");

            await LeaderService.UpdateTravelLeaderAsync(_model);
            await _accountService.UpdateAccountAsync(_model);
            Navigation.NavigateTo("/travelleaders");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            await JS.InvokeVoidAsync("window.scrollTo", 0, 0);
            StateHasChanged();
        }
    }
}
