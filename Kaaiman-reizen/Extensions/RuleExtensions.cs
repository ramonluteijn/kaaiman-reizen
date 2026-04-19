using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Models.ViewModels;

namespace Kaaiman_reizen.Extensions;

public static class RuleExtensions
{
    public static RuleViewModel ToViewModel(this Rule rule)
    {
        return new RuleViewModel
        {
            Id = rule.Id,
            Key = rule.Key,
            Description = rule.Description,
            TypedValue = rule.TypedValue,
            IsActive = rule.IsActive
        };
    }

    public static IReadOnlyList<RuleViewModel> ToViewModels(this IEnumerable<Rule> rules) =>
        rules.Select(ToViewModel).ToList();
}

