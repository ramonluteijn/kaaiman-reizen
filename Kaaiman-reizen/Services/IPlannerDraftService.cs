using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Models.Planner;

namespace Kaaiman_reizen.Services;

public interface IPlannerDraftService
{
    Task<PlannerDraftRequest> BuildRequestAsync(int year, CancellationToken ct = default);
    Task<PlannerDraftRequest> BuildRequestAsync(PlanningRound round, CancellationToken ct = default);
    PlannerDraftResult GenerateDraft(PlannerDraftRequest request);
}
