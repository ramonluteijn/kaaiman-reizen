using Kaaiman_reizen.Data.Entities;
using static Kaaiman_reizen.Data.Services.TravelLeaderService;

namespace Kaaiman_reizen.Data.Services;

public interface ITravelLeaderService
{
    Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TravelLeader>> GetTravelLeadersAsync(CancellationToken cancellationToken = default);
    Task AddTravelLeaderAsync(TravelLeader leader, CancellationToken cancellationToken = default);
    Task DeleteTravelLeaderAsync(int id, CancellationToken cancellationToken = default);
    Task<TravelLeader?> GetTravelLeaderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TravelLeader?> GetTravelLeaderByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task UpdateTravelLeaderAsync(TravelLeader leader, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(int leaderId, int? amountOfTrips, bool isActive, CancellationToken cancellationToken = default);

    Task<List<TravelLeader>> GetTravelLeadersWithoutPreferencesAsync();
    Task<List<TravelLeader>> GetTravelLeadersWithoutJourneysAsync(int year);
    Task<List<TravelLeader>> GetTravelLeadersWithNotesAsync();
    Task<List<Journey>> GetJourneysWithoutTravelLeadersAsync(int year);
    Task<List<OverlapData>> GetTravelLeadersWithOverlappingJourneys();
    Task<IReadOnlyList<Journey>> GetJourneysOfTravelLeaderAsync(TravelLeader leader, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TravelLeader>> GetJourneyAvailabilityForAllTravelLeadersAsync();
    Task<int> ArchiveAndResetPreferredDestinationsAsync(int? planningVersionId, CancellationToken cancellationToken = default);
    Task IncrementTravelLeadersExperience(int journeyId, CancellationToken cancellationToken);
}
