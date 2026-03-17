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

    public async Task<IReadOnlyList<Entities.TravelLeader>> GetTravelLeadersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.TravelLeader
            .Include(t => t.PreferredDestinations)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddTravelLeaderAsync(Entities.TravelLeader leader, CancellationToken cancellationToken = default)
    {
        _db.TravelLeader.Add(leader);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTravelLeaderAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.TravelLeader.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return;

        _db.TravelLeader.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Entities.TravelLeader?> GetTravelLeaderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.TravelLeader
            .Include(t => t.PreferredDestinations)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateTravelLeaderAsync(Entities.TravelLeader leader, CancellationToken cancellationToken = default)
    {
        var tracked = _db.ChangeTracker.Entries<Entities.TravelLeader>().FirstOrDefault(e => e.Entity.Id == leader.Id);
        if (tracked != null)
        {
            tracked.State = EntityState.Detached;
        }

        _db.TravelLeader.Update(leader);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
