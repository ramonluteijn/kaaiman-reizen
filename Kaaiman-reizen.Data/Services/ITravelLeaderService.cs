namespace Kaaiman_reizen.Data.Services;

public interface ITravelLeaderService
{
    Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.TravelLeader>> GetTravelLeadersAsync(CancellationToken cancellationToken = default);
    Task AddTravelLeaderAsync(Entities.TravelLeader leader, CancellationToken cancellationToken = default);
    Task DeleteTravelLeaderAsync(int id, CancellationToken cancellationToken = default);
    Task<Entities.TravelLeader?> GetTravelLeaderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateTravelLeaderAsync(Entities.TravelLeader leader, CancellationToken cancellationToken = default);
}
