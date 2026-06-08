using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class PlanningRoundService : IPlanningRoundService
{
    private readonly MainContext _db;

    public PlanningRoundService(MainContext db) => _db = db;

    public async Task<PlanningRound> CreateAsync(string name, int year, DateOnly startDate, DateOnly endDate,
        DateTime preferenceDeadline, DateTime publicationDeadline, CancellationToken ct = default)
    {
        var activeLeaderIds = await _db.TravelLeader
            .Where(l => l.IsActive)
            .Select(l => l.Id)
            .ToListAsync(ct);

        var round = new PlanningRound
        {
            Name = name,
            Year = year,
            StartDate = startDate,
            EndDate = endDate,
            PreferenceDeadline = preferenceDeadline,
            PublicationDeadline = publicationDeadline,
            Participations = activeLeaderIds.Select(id => new PlanningRoundParticipation
            {
                TravelLeaderId = id,
                Status = ParticipationStatus.Pending
            }).ToList()
        };

        _db.PlanningRounds.Add(round);
        await _db.SaveChangesAsync(ct);

        return round;
    }

    public async Task<IReadOnlyList<PlanningRound>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.PlanningRounds
            .Include(r => r.Participations)
            .Include(r => r.Versions)
            .OrderByDescending(r => r.Year)
            .ThenBy(r => r.StartDate)
            .ToListAsync(ct);
    }
}
