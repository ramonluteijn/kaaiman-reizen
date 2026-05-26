using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Kaaiman_reizen.Components.Pages.Archive;

public partial class Archive
{
    [Inject]
    private IPlanningService PlanningService { get; set; } = default!;

    [Inject]
    private AccountService _accountService { get; set; } = default!;

    [Inject]
    private IHostEnvironment HostEnvironment { get; set; } = default!;

    protected bool _loading = true;
    private string? _error;
    private List<PlanningVersion> _plans = [];
    private string _searchTerm = string.Empty;
    private string _sortColumn = "Name";
    private bool _sortAscending = true;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _plans = await PlanningService.GetPublishedPlansAsync();
        }
        catch (Exception ex)
        {
            _error = $"Kon reisleiders niet laden: {LoadErrorFormatter.Format(ex, HostEnvironment)}";
            _plans = [];
        }
        finally
        {
            _loading = false;
        }
    }

    private IEnumerable<PlanningVersion> FilteredPlans =>
        ApplySorting(
            _plans.Where(l =>
                string.IsNullOrWhiteSpace(_searchTerm) ||
                l.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)
            )
    );

    private IEnumerable<PlanningVersion> ApplySorting(IEnumerable<PlanningVersion> query)
    {
        return _sortColumn switch
        {
            "Name" => _sortAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            "CreatedAt" => _sortAscending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
            "PlanningYear" => _sortAscending ? query.OrderBy(x => x.PlanningYear) : query.OrderByDescending(x => x.PlanningYear),
            _ => query
        };
    }

    private void SortBy(string column)
    {
        if (_sortColumn == column) _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }
    }

    private string GetSortIcon(string column)
    {
        if (_sortColumn != column) return "";

        return _sortAscending ? "↑" : "↓";
    }

    private static string GetDetailHref(PlanningVersion planning)
    {
        return $"/archive/planning/{planning.Id}";
    }
}