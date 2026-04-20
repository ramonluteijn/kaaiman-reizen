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

    // SC-1: Alle ophaal-methodes hebben nu een 'int year' parameter nodig
    public Task<PlanningVersion?> GetLatestDraftAsync(int year, CancellationToken cancellationToken = default)
    {
        return GetLatestPlanningVersionAsync(year, isPublished: false, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanningVersion>> GetDraftsAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _db.PlanningVersions
            .Where(version => !version.IsPublished && version.PlanningYear == year)
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

    public Task<PlanningVersion?> GetLatestPublishedAsync(int year, CancellationToken cancellationToken = default)
    {
        return GetLatestPlanningVersionAsync(year, isPublished: true, cancellationToken);
    }

    // SC-2: Opslaan logica is drastisch verbeterd
    public async Task<PlanningVersion> SavePlanningAsync(
        int year,
        string name,
        bool isPublished,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> journeyAssignments,
        CancellationToken cancellationToken = default)
    {
        PlanningVersion version;

        if (isPublished)
        {
            // PUBLICEREN:
            // 1. De-activeer de huidige actieve live planning van DIT jaar
            var publishedVersions = await _db.PlanningVersions
                .Where(v => v.IsPublished && v.PlanningYear == year)
                .ToListAsync(cancellationToken);

            foreach (var publishedVersion in publishedVersions)
            {
                publishedVersion.IsPublished = false;
            }

            // 2. Maak altijd een NIEUW record aan bij publiceren, zodat we de historie behouden (SC-3)
            version = CreateNewVersionObject(year, name, true, journeyAssignments);
            _db.PlanningVersions.Add(version);
        }
        else
        {
            // CONCEPT OPSLAAN (D2 Fix - Eén concept per jaar):
            // 1. Kijk of er al een concept bestaat voor dit jaar
            version = await _db.PlanningVersions
                .Include(v => v.Assignments)
                .FirstOrDefaultAsync(v => !v.IsPublished && v.PlanningYear == year, cancellationToken);

            if (version == null)
            {
                // Bestaat niet? Maak hem aan.
                version = CreateNewVersionObject(year, name, false, journeyAssignments);
                _db.PlanningVersions.Add(version);
            }
            else
            {
                // Bestaat wel? Overschrijf de oude data (Update)
                version.Name = name;
                version.CreatedAt = DateTime.UtcNow; // Werk tijdstempel bij naar nu
                
                // Gooi oude toewijzingen weg...
                _db.RemoveRange(version.Assignments);
                
                // ...en zet de nieuwe erin
                version.Assignments = MapAssignments(journeyAssignments);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return version;
    }

    // Helper methode: Omdat we nu een filter hebben op PlanningYear
    private Task<PlanningVersion?> GetLatestPlanningVersionAsync(int year, bool isPublished, CancellationToken cancellationToken)
    {
        return _db.PlanningVersions
            .Where(version => version.IsPublished == isPublished && version.PlanningYear == year)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.TravelLeader)
            .Include(version => version.Assignments)
                .ThenInclude(assignment => assignment.Journey)
            .OrderByDescending(version => version.CreatedAt)
            .ThenByDescending(version => version.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Helper methode: Voorkomt dubbele code bij het aanmaken van een PlanningVersion
    private static PlanningVersion CreateNewVersionObject(
        int year, string name, bool isPublished, IReadOnlyDictionary<int, IReadOnlyCollection<int>> journeyAssignments)
    {
        return new PlanningVersion
        {
            PlanningYear = year,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            IsPublished = isPublished,
            Assignments = MapAssignments(journeyAssignments)
        };
    }

    // Helper methode: Mapt de dictionary naar EF Core objecten
    private static List<PlanningAssignment> MapAssignments(IReadOnlyDictionary<int, IReadOnlyCollection<int>> dict)
    {
        return dict.SelectMany(
            pair => pair.Value.Distinct(),
            (pair, leaderId) => new PlanningAssignment
            {
                JourneyId = pair.Key,
                TravelLeaderId = leaderId
            }).ToList();
    }
}