using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data.Services;

public interface IPlanningRoundService
{
    Task<PlanningRound> CreateAsync(string name, int year, DateOnly startDate, DateOnly endDate,
        DateTime preferenceDeadline, DateTime publicationDeadline, CancellationToken ct = default);

    Task<IReadOnlyList<PlanningRound>> GetAllAsync(CancellationToken ct = default);
    Task<PlanningRound?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PlanningRoundParticipation>> GetParticipationsForLeaderAsync(int leaderId, CancellationToken ct = default);
    Task SavePreferencesAsync(int participationId, IReadOnlyList<(int JourneyId, int Rank)> preferences, CancellationToken ct = default);
    Task MarkUnavailableAsync(int participationId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
