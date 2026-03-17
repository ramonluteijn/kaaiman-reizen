namespace Kaaiman_reizen.Data.Services;

public interface ITravelLeaderService
{
    Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default);
}
