using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Rules;

public static class RuleQueryExtensions
{
    public static async Task<bool> IsRuleEnabledAsync(
        this MainContext db,
        string key,
        bool defaultValue,
        CancellationToken cancellationToken = default)
    {
        var rule = await db.Rule
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (rule == null)
            return defaultValue;

        return bool.TryParse(rule.Value, out var isEnabled)
            ? isEnabled
            : defaultValue;
    }
}
