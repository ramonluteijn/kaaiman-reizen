using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Identity;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

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

    [Inject]
    private UserManager<ApplicationUser> _userManager { get; set; } = default!;

    private string? _errorMessage;

    private TravelLeader _model = new();
    private string _email = string.Empty;
    private string[] _preferred = new string[3];
    private List<PeriodModel> _periods = new();

    private class PeriodModel
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    private void AddPeriod()
    {
        _periods.Add(new PeriodModel());
    }

    private void RemovePeriod(int index)
    {
        if (index >= 0 && index < _periods.Count)
            _periods.RemoveAt(index);
    }

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

            var existingIdentityEmail = await _userManager.FindByEmailAsync(_model.Email);
            var existingIdentityPhone = await _userManager.Users
                .AnyAsync(u => u.PhoneNumber == _model.PhoneNumber);

            if (existingLeader is not null || existingIdentityEmail is not null || existingIdentityPhone)
            {
                if (existingLeader?.Email == _model.Email || existingIdentityEmail is not null)
                    throw new Exception($"Het e-mailadres {_model.Email} is al in gebruik.");

                if (existingLeader?.PhoneNumber == _model.PhoneNumber || existingIdentityPhone)
                    throw new Exception($"Het telefoonnummer {_model.PhoneNumber} is al in gebruik.");
            }

            _model.PreferredDestinations = _preferred
                .Select((dest, index) => new { dest, index })
                .Where(x => !string.IsNullOrWhiteSpace(x.dest))
                .Take(3)
                .Select(x => new PreferredDestination { Rank = x.index + 1, Destination = x.dest })
                .ToList();

            _model.AvailabilityPeriods = _periods
                .Where(p => p.Start.HasValue && p.End.HasValue)
                .Select(p => new AvailabilityPeriod
                {
                    Start = DateOnly.FromDateTime(p.Start!.Value),
                    End = DateOnly.FromDateTime(p.End!.Value)
                })
                .ToList();

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
