using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class JourneyService : IJourneyService
{
    private readonly MainContext _db;
    private readonly IPlanningService _planningService;

    public JourneyService(MainContext db, IPlanningService planningService)
    {
        _db = db;
        _planningService = planningService;
    }

    public async Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.TravelLeader
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Entities.Journey>> GetJourneysAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Journey
            .Include(j => j.TravelLeaders)
            .OrderBy(j => j.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Journey>> GetJourneysWithPublishedPlanningAsync(CancellationToken cancellationToken = default)
    {
        var journeys = await GetJourneysAsync(cancellationToken);
        var publishedPlanning = await _planningService.GetLatestPublishedAsync(cancellationToken);

        return ApplyPlanningVersion(journeys, publishedPlanning);
    }

    public async Task AddJourneyAsync(Entities.Journey journey, List<int> selectedLeaders, CancellationToken cancellationToken = default)
    {
        var leaders = await _db.TravelLeader
            .Where(t => selectedLeaders.Contains(t.Id))
            .ToListAsync();

        journey.TravelLeaders = leaders;

        _db.Journey.Add(journey);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteJourneyAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Journey.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return;

        _db.Journey.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Entities.Journey?> GetJourneyByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Journey
            .Include(j => j.TravelLeaders)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<Journey?> GetJourneyByIdWithPublishedPlanningAsync(int id, CancellationToken cancellationToken = default)
    {
        var journey = await GetJourneyByIdAsync(id, cancellationToken);
        if (journey is null)
        {
            return null;
        }

        var publishedPlanning = await _planningService.GetLatestPublishedAsync(cancellationToken);
        return ApplyPlanningVersion([journey], publishedPlanning).SingleOrDefault();
    }

    public async Task UpdateJourneyAsync(Entities.Journey journey, List<int> selectedLeaders, CancellationToken cancellationToken = default)
    {
        var tracked = _db.ChangeTracker.Entries<Entities.Journey>().FirstOrDefault(e => e.Entity.Id == journey.Id);
        if (tracked != null)
        {
            tracked.State = EntityState.Detached;
        }

        _db.Attach(journey);
        _db.Entry(journey).State = EntityState.Modified;

        var leaders = await _db.TravelLeader
           .Where(t => selectedLeaders.Contains(t.Id))
           .ToListAsync(cancellationToken);

        journey.TravelLeaders.Clear();

        foreach (var leader in leaders)
        {
            journey.TravelLeaders.Add(leader);
        }

        _db.Journey.Update(journey);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<Journey> ApplyPlanningVersion(
        IReadOnlyList<Journey> journeys,
        PlanningVersion? planningVersion)
    {
        if (planningVersion is null)
        {
            return journeys;
        }

        var assignmentsByJourneyId = planningVersion.Assignments
            .GroupBy(assignment => assignment.JourneyId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(assignment => assignment.TravelLeader)
                    .OrderBy(leader => leader.Name)
                    .ToList());

        foreach (var journey in journeys)
        {
            journey.TravelLeaders = assignmentsByJourneyId.TryGetValue(journey.Id, out var leaders)
                ? leaders
                : [];
        }

        return journeys;
    }
}
