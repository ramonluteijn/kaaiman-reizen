namespace Kaaiman_reizen.Data.Services;

public interface IRuleService
{
    Task<IReadOnlyList<string>> GetRuleKeysAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Rule>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<Entities.Rule?> GetRuleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateRuleAsync(Entities.Rule rule, CancellationToken cancellationToken = default);
}
