using Kaaiman_reizen.Data.Services;
using Kaaiman_reizen.Data.Rules;
using Kaaiman_reizen.Extensions;
using Kaaiman_reizen.Helpers;
using Kaaiman_reizen.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kaaiman_reizen.Components.Pages.Rules;

public partial class Rules
{
    [Inject]
    private IRuleService RuleService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ILogger<Rules> Logger { get; set; } = default!;

    [Inject]
    private IHostEnvironment HostEnvironment { get; set; } = default!;

    protected bool _loading = true;
    private string? _error;
    private List<RuleViewModel> _rules = [];
    private string _searchTerm = string.Empty;
    private string _sortColumn = "Key";
    private bool _sortAscending = true;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var items = await RuleService.GetRulesAsync();
            _rules = items.ToViewModels().ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Kon regels niet laden");
            _error = $"Kon regels niet laden: {LoadErrorFormatter.Format(ex, HostEnvironment)}";
            _rules = [];
        }
        finally
        {
            _loading = false;
        }
    }

    private IEnumerable<RuleViewModel> FilteredRules =>
        ApplySorting(
            _rules.Where(r =>
                string.IsNullOrWhiteSpace(_searchTerm) ||
                r.Key.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)
            )
    );

    private IEnumerable<RuleViewModel> ApplySorting(IEnumerable<RuleViewModel> query)
    {
        return _sortColumn switch
        {
            "Key" => _sortAscending ? query.OrderBy(x => x.Key) : query.OrderByDescending(x => x.Key),
            "Active" => _sortAscending ? query.OrderBy(x => x.IsActive) : query.OrderByDescending(x => x.IsActive),
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

    private static bool HasEditableValue(RuleViewModel rule)
    {
        return rule.Key.Equals(RuleKeys.MinimumGapDays, StringComparison.OrdinalIgnoreCase)
               || rule.Key.Equals(RuleKeys.RequiredExperience, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRuleValue(RuleViewModel rule)
    {
        return rule.TypedValue?.ToString() ?? string.Empty;
    }

    private async Task UpdateRuleValue(RuleViewModel? rule, string? newValue)
    {
        if (rule == null || !HasEditableValue(rule))
            return;

        var entity = await RuleService.GetRuleByIdAsync(rule.Id);
        if (entity == null)
            return;

        entity.TypedValue = int.TryParse(newValue, out var intValue) ? intValue : newValue;
        await RuleService.UpdateRuleAsync(entity);

        var idx = _rules.FindIndex(r => r.Id == rule.Id);
        if (idx >= 0)
        {
            _rules[idx] = entity.ToViewModel();
            StateHasChanged();
        }
    }

    private async Task UpdateRuleWeight(RuleViewModel? rule, string? newValue)
    {
        if (rule == null)
            return;

        if (!int.TryParse(newValue, out var weight))
            weight = 1;

        var entity = await RuleService.GetRuleByIdAsync(rule.Id);
        if (entity == null)
            return;

        entity.Weight = weight;
        await RuleService.UpdateRuleAsync(entity);

        var idx = _rules.FindIndex(r => r.Id == rule.Id);
        if (idx >= 0)
        {
            _rules[idx] = entity.ToViewModel();
            StateHasChanged();
        }
    }

    private async Task ToggleActive(RuleViewModel? rule)
    {
        if (rule == null)
            return;

        var entity = await RuleService.GetRuleByIdAsync(rule.Id);
        if (entity != null)
        {
            entity.IsActive = !entity.IsActive;
            await RuleService.UpdateRuleAsync(entity);

            var idx = _rules.FindIndex(l => l.Id == rule.Id);
            if (idx >= 0)
            {
                _rules[idx] = entity.ToViewModel();
                StateHasChanged();
            }
        }
    }
}