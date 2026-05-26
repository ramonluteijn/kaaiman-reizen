using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data.Services;

public interface IPlanningService
{
    Task<IReadOnlyList<PlanningVersion>> GetDraftsAsync(int year, CancellationToken cancellationToken = default);
    Task<PlanningVersion?> GetPlanningVersionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PlanningVersion?> GetLatestDraftAsync(int year, CancellationToken cancellationToken = default);
    Task<PlanningVersion?> GetLatestPublishedAsync(int year, CancellationToken cancellationToken = default);
    Task<List<PlanningVersion>> GetPublishedPlansAsync(CancellationToken cancellationToken = default);
    Task<PlanningVersion> SavePlanningAsync(
        int year,
        string name,
        bool isPublished,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> journeyAssignments,
        CancellationToken cancellationToken = default);
    Task<List<Journey>> GetAllJourneysWithTravelLeadersFromLatestPublishedPlanning();
    Task<List<Journey>> GetAllJourneysOfPlanningByIdAsync(int id, CancellationToken cancellationToken = default);

    bool PublishedPlanningExists();
    Task<int?> GetLatestPublishedPlanningVersionIdAsync(CancellationToken cancellationToken = default);
}