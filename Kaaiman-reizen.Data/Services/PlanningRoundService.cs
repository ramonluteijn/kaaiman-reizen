using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Dtos;
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

        var activeLeaders = await _db.TravelLeader
            .Where(l => l.IsActive)
            .Select(l => new { l.Id, l.MinTrips, l.MaxTrips, l.Note })
            .ToListAsync(ct);

        var round = new PlanningRound
        {
            Name = name,
            Year = year,
            StartDate = startDate,
            EndDate = endDate,
            PreferenceDeadline = preferenceDeadline,
            PublicationDeadline = publicationDeadline,
            Participations = activeLeaders.Select(leader => new PlanningRoundParticipation
            {
                TravelLeaderId = leader.Id,
                MinTrips = leader.MinTrips,
                MaxTrips = leader.MaxTrips,
                Note = leader.Note,
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
                .ThenInclude(p => p.TravelLeader)
            .Include(r => r.Participations)
                .ThenInclude(p => p.Preferences)
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

    public async Task SavePreferencesAsync(
        int participationId,
        IReadOnlyList<(int JourneyId, int Rank)> preferences,
        int? minTrips,
        int? maxTrips,
        string note,
        bool allowAfterDeadline = false,
        CancellationToken ct = default)
    {
        if (minTrips.HasValue && maxTrips.HasValue && minTrips > maxTrips)
            throw new InvalidOperationException("Minimaal aantal reizen mag niet groter zijn dan maximaal aantal reizen.");

        var participation = await _db.PlanningRoundParticipations
            .Include(p => p.Preferences)
            .Include(p => p.PlanningRound)
            .FirstOrDefaultAsync(p => p.Id == participationId, ct);

        if (participation is null) return;

        if (!allowAfterDeadline && participation.PlanningRound.IsPreferenceDeadlinePassed())
            throw new InvalidOperationException("De deadline voor het indienen van voorkeuren is verlopen.");

        _db.RemoveRange(participation.Preferences);
        participation.Preferences = preferences.Select(p => new PlanningRoundPreference
        {
            PlanningRoundParticipationId = participationId,
            JourneyId = p.JourneyId,
            Rank = p.Rank
        }).ToList();
        participation.MinTrips = minTrips;
        participation.MaxTrips = maxTrips;
        participation.Note = note;

        participation.Status = ParticipationStatus.Submitted;
        participation.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ParticipationNoteEntry>> GetParticipationNotesAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.PlanningRoundParticipations
            .Where(p =>
                p.PlanningRound.EndDate >= today &&
                p.Note != null &&
                p.Note != string.Empty)
            .Select(p => new ParticipationNoteEntry
            {
                PlanningRoundId = p.PlanningRoundId,
                PlanningRoundName = p.PlanningRound.Name,
                TravelLeaderId = p.TravelLeaderId,
                TravelLeaderName = p.TravelLeader.Name,
                Note = p.Note
            })
            .OrderBy(x => x.PlanningRoundName)
            .ThenBy(x => x.TravelLeaderName)
            .ToListAsync(ct);
    }

    public async Task MarkUnavailableAsync(int participationId, CancellationToken ct = default)
    {
        var participation = await _db.PlanningRoundParticipations
            .Include(p => p.Preferences)
            .FirstOrDefaultAsync(p => p.Id == participationId, ct);

        if (participation is null) return;

        _db.RemoveRange(participation.Preferences);
        participation.Preferences = [];
        participation.Status = ParticipationStatus.Unavailable;
        participation.SubmittedAt = DateTime.UtcNow;

        // Remove leader from any unsaved draft planning versions for this round
        var draftAssignments = await _db.PlanningAssignments
            .Where(a => a.TravelLeaderId == participation.TravelLeaderId
                     && a.PlanningVersion.PlanningRoundId == participation.PlanningRoundId
                     && !a.PlanningVersion.IsPublished)
            .ToListAsync(ct);

        _db.RemoveRange(draftAssignments);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var round = await _db.PlanningRounds.FindAsync(new object[] { id }, ct);
        if (round is null) return;

        _db.PlanningRounds.Remove(round);
        await _db.SaveChangesAsync(ct);
    }

    public async Task VerifyLeadersAsync(int roundId, CancellationToken ct = default)
    {
        await _db.PlanningRounds
            .Where(r => r.Id == roundId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LeadersVerifiedAt, DateTime.UtcNow), ct);
    }
}
