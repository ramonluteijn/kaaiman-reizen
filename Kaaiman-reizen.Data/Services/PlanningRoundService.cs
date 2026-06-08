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
        var overlapping = await _db.PlanningRounds
            .Where(r => r.StartDate <= endDate && startDate <= r.EndDate)
            .FirstOrDefaultAsync(ct);

        if (overlapping is not null)
            throw new InvalidOperationException(
                $"De datumreeks overlapt met bestaande ronde \"{overlapping.Name}\" " +
                $"({overlapping.StartDate:d MMM} – {overlapping.EndDate:d MMM yyyy}). Kies een andere periode.");

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

    public Task<PlanningRound?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _db.PlanningRounds
            .Include(r => r.Participations)
                .ThenInclude(p => p.Preferences)
                    .ThenInclude(pref => pref.Journey)
            .Include(r => r.Versions)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<PlanningRoundParticipation>> GetParticipationsForLeaderAsync(int leaderId, CancellationToken ct = default)
    {
        return await _db.PlanningRoundParticipations
            .Where(p => p.TravelLeaderId == leaderId)
            .Include(p => p.PlanningRound)
            .Include(p => p.Preferences)
                .ThenInclude(pref => pref.Journey)
            .OrderBy(p => p.PlanningRound.PreferenceDeadline)
            .ToListAsync(ct);
    }

    public async Task SavePreferencesAsync(int participationId, IReadOnlyList<(int JourneyId, int Rank)> preferences, CancellationToken ct = default)
    {
        var participation = await _db.PlanningRoundParticipations
            .Include(p => p.Preferences)
            .FirstOrDefaultAsync(p => p.Id == participationId, ct);

        if (participation is null) return;

        _db.RemoveRange(participation.Preferences);
        participation.Preferences = preferences.Select(p => new PlanningRoundPreference
        {
            PlanningRoundParticipationId = participationId,
            JourneyId = p.JourneyId,
            Rank = p.Rank
        }).ToList();

        participation.Status = ParticipationStatus.Submitted;
        participation.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var round = await _db.PlanningRounds.FindAsync(new object[] { id }, ct);
        if (round is null) return;

        _db.PlanningRounds.Remove(round);
        await _db.SaveChangesAsync(ct);
    }
}
