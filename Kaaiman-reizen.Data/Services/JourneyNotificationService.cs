using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Rules;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class JourneyNotificationService
{
    private readonly MainContext _db;
    private readonly IEmailDispatcher _emailDispatcher;
    private readonly INotificationService _notificationService;

    public JourneyNotificationService(
        MainContext db,
        IEmailDispatcher emailDispatcher,
        INotificationService notificationService)
    {
        _db = db;
        _emailDispatcher = emailDispatcher;
        _notificationService = notificationService;
    }

    public async Task SendJourneyRemindersAsync(CancellationToken cancellationToken = default)
    {
        var isReminderEnabled = await IsReminderEnabledAsync(cancellationToken);
        if (!isReminderEnabled)
            return;

        var reminderDays = await GetReminderDaysAsync(cancellationToken);
        if (reminderDays.Count == 0)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var days in reminderDays)
        {
            var targetDate = today.AddDays(days);

            var journeys = await _db.Journey
                .Where(j => j.Start == targetDate)
                .Include(j => j.TravelLeaders)
                .ToListAsync(cancellationToken);

            foreach (var journey in journeys)
            {
                foreach (var travelLeader in journey.TravelLeaders)
                {
                    var applicationUser = await _db.Users
                        .FirstOrDefaultAsync(u => u.Email == travelLeader.Email, cancellationToken);

                    if (applicationUser != null)
                    {
                        var alreadySent = await _db.JourneyNotificationHistory
                            .AnyAsync(
                                h => h.JourneyId == journey.Id &&
                                     h.ApplicationUserId == applicationUser.Id &&
                                     h.DaysBeforeStart == days,
                                cancellationToken);

                        if (alreadySent)
                            continue;
                    }

                    var dashboardMessage = $"Uw reis naar {journey.Name} start op {journey.Start:dd-MM-yyyy}.";
                    var emailSubject = $"Herinnering: Uw reis naar {journey.Name}";
                    var emailBody = $@"
                        <html>
                            <body>
                                <p>Beste, {travelLeader.Name}</p>
                                <p>Dit is een herinnering dat uw reis naar <strong>{journey.Name}</strong> over {days} dag(en) start op <strong>{journey.Start:dd-MM-yyyy}</strong>.</p>
                                <p>Tot ziens!</p>
                            </body>
                        </html>";

                    await _emailDispatcher.SendEmailAsync(travelLeader.Email, emailSubject, emailBody);

                    // Only create dashboard notification and history if user exists
                    if (applicationUser != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            applicationUser.Id,
                            dashboardMessage,
                            cancellationToken);

                        var history = new JourneyNotificationHistory
                        {
                            JourneyId = journey.Id,
                            ApplicationUserId = applicationUser.Id,
                            DaysBeforeStart = days,
                            SentAt = DateTime.UtcNow
                        };

                        _db.JourneyNotificationHistory.Add(history);
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsReminderEnabledAsync(CancellationToken cancellationToken)
    {
        var rule = await _db.Rule
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == RuleKeys.JourneyReminderEnabled, cancellationToken);

        if (rule == null)
            return RuleKeys.DefaultJourneyReminderEnabled;

        return bool.TryParse(rule.Value, out var isEnabled)
            ? isEnabled
            : RuleKeys.DefaultJourneyReminderEnabled;
    }

    private async Task<List<int>> GetReminderDaysAsync(CancellationToken cancellationToken)
    {
        var rule = await _db.Rule
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == RuleKeys.JourneyReminderDays, cancellationToken);

        var configuredDays = rule?.Value;
        if (string.IsNullOrWhiteSpace(configuredDays))
            return [.. RuleKeys.DefaultJourneyReminderDays];

        var parsedDays = configuredDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var day) ? day : (int?)null)
            .Where(day => day.HasValue && day.Value > 0)
            .Select(day => day!.Value)
            .Distinct()
            .OrderByDescending(day => day)
            .ToList();

        return parsedDays.Count > 0
            ? parsedDays
            : [.. RuleKeys.DefaultJourneyReminderDays];
    }
}
