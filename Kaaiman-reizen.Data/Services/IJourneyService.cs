namespace Kaaiman_reizen.Data.Services;

public interface IJourneyService
{
    Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Journey>> GetJourneysAsync(CancellationToken cancellationToken = default);
    Task AddJourneyAsync(Entities.Journey journey, List<int>  selectedLeaders, CancellationToken cancellationToken = default);
    Task DeleteJourneyAsync(int id, CancellationToken cancellationToken = default);
    Task<Entities.TravelLeader?> GetTravelLeaderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateJourneyAsync(Entities.Journey journey, CancellationToken cancellationToken = default);
}
