using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class PlanningService : IPlanningService
{
    private readonly MainContext _db;

    public PlanningService(MainContext db)
    {
        _db = db;
    }

    public Task<PlanningVersion?> GetLatestDraftAsync(CancellationToken cancellationToken = default)
    {
        return GetLatestPlanningVersionAsync(isPublished: false, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanningVersion>> GetDraftsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PlanningVersions
            .Where(version => !version.IsPublished)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.TravelLeader)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.Journey)
            .OrderByDescending(version => version.CreatedAt)
            .ThenByDescending(version => version.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<PlanningVersion?> GetPlanningVersionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.PlanningVersions
            .Where(version => version.Id == id)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.TravelLeader)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.Journey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PlanningVersion?> GetLatestPublishedAsync(CancellationToken cancellationToken = default)
    {
        return GetLatestPlanningVersionAsync(isPublished: true, cancellationToken);
    }

    public async Task<PlanningVersion> SavePlanningAsync(
        string name,
        bool isPublished,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> journeyAssignments,
        CancellationToken cancellationToken = default)
    {
        if (isPublished)
        {
            var publishedVersions = await _db.PlanningVersions
                .Where(version => version.IsPublished)
                .ToListAsync(cancellationToken);

            foreach (var publishedVersion in publishedVersions)
            {
                publishedVersion.IsPublished = false;
            }
        }

        var version = new PlanningVersion
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            IsPublished = isPublished,
            Assignments = journeyAssignments
                .SelectMany(
                    pair => pair.Value.Distinct(),
                    (pair, leaderId) => new PlanningAssignment
                    {
                        JourneyId = pair.Key,
                        TravelLeaderId = leaderId
                    })
                .ToList()
        };

        _db.PlanningVersions.Add(version);
        await _db.SaveChangesAsync(cancellationToken);

        return version;
    }

    private Task<PlanningVersion?> GetLatestPlanningVersionAsync(bool isPublished, CancellationToken cancellationToken)
    {
        return _db.PlanningVersions
            .Where(version => version.IsPublished == isPublished)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.TravelLeader)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.Journey)
            .OrderByDescending(version => version.CreatedAt)
            .ThenByDescending(version => version.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
