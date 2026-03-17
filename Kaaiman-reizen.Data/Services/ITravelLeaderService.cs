using Kaaiman_reizen.Data.Entities;

namespace Kaaiman_reizen.Data.Services;

public interface ITravelLeaderService
{
    Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TravelLeader>> GetLeadersWithAvailabilityAsync(CancellationToken cancellationToken = default);
}
