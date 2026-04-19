using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class RuleService : IRuleService
{
    private readonly MainContext _db;

    public RuleService(MainContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetRuleKeysAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Rule
            .OrderBy(t => t.Key)
            .Select(t => t.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Entities.Rule>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Rule
            .OrderBy(t => t.Key)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Entities.Rule?> GetRuleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Rule
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateRuleAsync(Entities.Rule rule, CancellationToken cancellationToken = default)
    {
        _db.Rule.Update(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
