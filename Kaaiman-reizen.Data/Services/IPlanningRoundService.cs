using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data.Services;

public interface IPlanningRoundService
{
    Task<PlanningRound> CreateAsync(string name, int year, DateOnly startDate, DateOnly endDate,
        DateTime preferenceDeadline, DateTime publicationDeadline, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningRound>> GetAllAsync(CancellationToken ct = default);
}
