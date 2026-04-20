using Kaaiman_reizen.Models.Planner;

namespace Kaaiman_reizen.Services;

public interface IPlannerDraftService
{
    Task<PlannerDraftRequest> BuildRequestAsync(int year, CancellationToken ct = default);
    PlannerDraftResult GenerateDraft(PlannerDraftRequest request);
}
