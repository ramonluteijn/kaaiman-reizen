using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class TravelLeaderService : ITravelLeaderService
{
    private readonly MainContext _db;

    public TravelLeaderService(MainContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.TravelLeader
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
