using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.TravelLeaders.Edit;

public partial class Edit : ComponentBase
{
    [Inject]
    private ITravelLeaderService LeaderService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int Id { get; set; }

    private TravelLeader _model = new();
    private bool _loading = true;
    private bool _notFound = false;

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
        await LeaderService.UpdateTravelLeaderAsync(_model);
        Navigation.NavigateTo("/travelleaders");
    }
}
