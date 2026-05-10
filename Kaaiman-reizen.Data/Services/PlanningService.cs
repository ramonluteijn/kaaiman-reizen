using Kaaiman_reizen.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class PlanningService : IPlanningService
{
    private readonly MainContext _db;
    private readonly IServiceProvider _serviceProvider;

    public PlanningService(MainContext db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }
    
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
            var publishedVersions = await _db.PlanningVersions
                .Where(v => v.IsPublished && v.PlanningYear == year)
                .ToListAsync(cancellationToken);

            foreach (var publishedVersion in publishedVersions)
            {
                publishedVersion.IsPublished = false;
            }
            version = CreateNewVersionObject(year, name, true, journeyAssignments);
            _db.PlanningVersions.Add(version);

            var allUsers = await _db.Users.ToListAsync(cancellationToken);
            var notifications = allUsers.Select(u => new Notification
            {
                ApplicationUserId = u.Id,
                Message = $"De definitieve planning voor {year} is gepubliceerd. Bekijk het dashboard en geef uw input.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            _db.Notifications.AddRange(notifications);

            var emails = allUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)).Select(u => u.Email!).ToList();
            if (emails.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(_serviceProvider);
                        var emailDispatcher = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IEmailDispatcher>(scope.ServiceProvider);
                        var subject = $"Nieuwe planning gepubliceerd voor {year}";
                        var message = $"De definitieve planning voor {year} is gepubliceerd. Log in op het dashboard om de planning te bekijken en uw input te geven.";
                        await emailDispatcher.SendEmailToUsersAsync(emails, subject, message);
                    }
                    catch
                    {
                        
                    }
                });
            }
        }
        else
        {
            version = await _db.PlanningVersions
                .Include(v => v.Assignments)
                .FirstOrDefaultAsync(v => !v.IsPublished && v.PlanningYear == year, cancellationToken);

            if (version == null)
            {
                version = CreateNewVersionObject(year, name, false, journeyAssignments);
                _db.PlanningVersions.Add(version);
            }
            else
            {
                // Bestaat wel? Overschrijf de oude data (Update)
                version.Name = name;
                version.CreatedAt = DateTime.UtcNow; // Werk tijdstempel bij naar nu
                _db.RemoveRange(version.Assignments);
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

   public async Task<List<Journey>> GetAllJourneysWithTravelLeadersFromLatestPublishedPlanning()
   {
      List<Journey> result = new();

      int? latestPlanningId = await _db.PlanningVersions
         .Where(planning => planning.IsPublished)
         .OrderByDescending(planning => planning.CreatedAt)
         .Select(planning => planning.Id)
         .FirstOrDefaultAsync();

        if (latestPlanningId == 0)
        {
            return result;
        }

        var assignments = await _db.PlanningAssignments
         .Where(a => a.PlanningVersionId == latestPlanningId)
         .Include(a => a.Journey)
         .Include(a => a.TravelLeader)
         .ToListAsync();

        result = assignments
            .GroupBy(a => a.JourneyId)
            .Select(g =>
            {
                var journey = g.First().Journey;
                journey.TravelLeaders = g.Select(a => a.TravelLeader).ToList();
                return journey;
            }).ToList();

        return result;
    }

    public bool PublishedPlanningExists() => _db.PlanningVersions.Any(planning => planning.IsPublished);

}