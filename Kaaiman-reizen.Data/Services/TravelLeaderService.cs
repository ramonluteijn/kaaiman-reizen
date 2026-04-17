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
            .Include(t => t.AvailabilityPeriods)
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

        var assignments = await _db.PlanningAssignments
            .Where(pa => pa.TravelLeaderId == id)
            .ToListAsync(cancellationToken);
        _db.PlanningAssignments.RemoveRange(assignments);
        
        _db.TravelLeader.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Entities.TravelLeader?> GetTravelLeaderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.TravelLeader
            .Include(t => t.PreferredDestinations)
            .Include(t => t.AvailabilityPeriods)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateTravelLeaderAsync(Entities.TravelLeader leader, CancellationToken cancellationToken = default)
    {
        // Explicitly remove old child records so they don't accumulate as orphans.
        // EF Core's Update() inserts new child entities (Id=0) but never deletes the old ones.
        var oldPeriods = await _db.AvailabilityPeriods
            .Where(a => a.TravelLeaderId == leader.Id)
            .ToListAsync(cancellationToken);
        _db.AvailabilityPeriods.RemoveRange(oldPeriods);

        var oldPrefs = await _db.PreferredDestinations
            .Where(p => p.TravelLeaderId == leader.Id)
            .ToListAsync(cancellationToken);
        _db.PreferredDestinations.RemoveRange(oldPrefs);

        var tracked = _db.ChangeTracker.Entries<Entities.TravelLeader>().FirstOrDefault(e => e.Entity.Id == leader.Id);
        if (tracked != null)
            tracked.State = EntityState.Detached;

        _db.TravelLeader.Update(leader);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
