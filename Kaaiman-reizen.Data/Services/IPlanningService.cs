using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data.Services;

public interface IPlanningService
{
    Task<IReadOnlyList<PlanningVersion>> GetDraftsAsync(CancellationToken cancellationToken = default);
    Task<PlanningVersion?> GetPlanningVersionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PlanningVersion?> GetLatestDraftAsync(CancellationToken cancellationToken = default);
    Task<PlanningVersion?> GetLatestPublishedAsync(CancellationToken cancellationToken = default);
    Task<PlanningVersion> SavePlanningAsync(
        string name,
        bool isPublished,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>> journeyAssignments,
        CancellationToken cancellationToken = default);
}
