using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Create;

public partial class TravelLeadersCreate : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private AccountService _accountService { get; set; } = default!;

    [Inject]
    private MainContext _db { get; set; } = default!;

    private string? _errorMessage;

    private TravelLeader _model = new();

    private void Cancel()
    {
        Navigation.NavigateTo("/travelleaders");
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            var existingLeader = await _db.TravelLeader
                .Where(tl => tl.Email == _model.Email || tl.PhoneNumber == _model.PhoneNumber)
                .Select(tl => new { tl.Email, tl.PhoneNumber })
                .FirstOrDefaultAsync();

            if (existingLeader is not null)
            {
                if (existingLeader.Email == _model.Email)
                    throw new Exception($"Het e-mailadres {_model.Email} is al in gebruik.");

                if (existingLeader.PhoneNumber == _model.PhoneNumber)
                    throw new Exception($"Het telefoonnummer {_model.PhoneNumber} is al in gebruik.");
            }

            await LeaderService.AddTravelLeaderAsync(_model);
            await _accountService.CreateIdentityUserForTravelLeaderAsync(_model.Email, _model.PhoneNumber, _model.Name, _model.Id);

            Navigation.NavigateTo("/travelleaders");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            _model.Id = 0;

            await JS.InvokeVoidAsync("window.scrollTo", 0, 0);
            StateHasChanged();
        }
    }
}
