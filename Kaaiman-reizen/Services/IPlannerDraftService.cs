using Kaaiman_reizen.Models;

namespace Kaaiman_reizen.Services;

public interface IPlannerDraftService
{
    PlannerDraftResult GenerateDraft(PlannerDraftRequest request);
}
