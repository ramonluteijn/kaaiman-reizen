using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class TravelLeaderService : ITravelLeaderService
{
    private readonly MainContext _db;
    private const string EMPTYSTRING = "";

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

    public async Task<List<TravelLeader>> GetTravelLeadersWithoutPreferencesAsync()
    {
        return await _db.TravelLeader
            .Where(travelLeader => travelLeader.AvailabilityPeriods.Any() == false && travelLeader.PreferredDestinations.Any() == false)
            .ToListAsync();
    }

    public async Task<List<TravelLeader>> GetTravelLeadersWithoutJourneysAsync()
    {
        return await _db.TravelLeader
            .Where(travelLeader => travelLeader.Journeys.Any() == false)
            .ToListAsync();
    }

    public async Task<List<TravelLeader>> GetTravelLeadersWithNotesAsync()
    {
        return await _db.TravelLeader
            .Where(travelLeader => travelLeader.Note != null && travelLeader.Note != EMPTYSTRING)
            .ToListAsync();
    }

    public async Task<List<Journey>> GetJourneysWithoutTravelLeadersAsync()
    {
        return await _db.Journey
            .Include(journey => journey.TravelLeaders)
            .Where(journey => journey.TravelLeaders.Any() == false)
            .ToListAsync();
    }

    public async Task<List<OverlapData>> GetTravelLeadersWithOverlappingJourneys()
    {
        var travelLeadersWithJourneys = await _db.TravelLeader
            .Include(tl => tl.Journeys)
            .ToListAsync();

        List<OverlapData> overlaps = new ();

        travelLeadersWithJourneys.ForEach(travelLeader =>
        {
            List<Journey> journeys = travelLeader.Journeys;

            for (int outer = 0; outer < journeys.Count; outer++)
            {
                for (int inner = outer + 1; inner < journeys.Count; inner++)
                {
                    var subjectJourney = journeys[outer];
                    var comparisonJourney = journeys[inner];

                    if (subjectJourney.Start < comparisonJourney.End && comparisonJourney.Start < subjectJourney.End)
                    {
                        overlaps.Add(new OverlapData()
                        {
                            travelLeader = travelLeader,
                            subjectJourney = subjectJourney,
                            overlappingJourney = comparisonJourney
                        });
                    }
                }
            }
        });

        return overlaps;
    }

    public class OverlapData
    {
        public TravelLeader travelLeader { get; set; }
        public Journey subjectJourney { get; set; }
        public Journey overlappingJourney { get; set; }
    }
}
