using Kaaiman_reizen.Data.Services;
using Microsoft.Extensions.Configuration;

namespace Kaaiman_reizen.Services;

public class JourneyReminderHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JourneyReminderHostedService> _logger;
    private readonly IConfiguration _configuration;

    public JourneyReminderHostedService(
        IServiceProvider serviceProvider,
        ILogger<JourneyReminderHostedService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JourneyReminderHostedService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get interval from config (default: 86400 seconds = 24 hours)
                var intervalSeconds = _configuration.GetValue<int>("JourneyReminder:IntervalSeconds", 86400);
                var delay = TimeSpan.FromSeconds(intervalSeconds);

                _logger.LogInformation("JourneyReminderHostedService: Next run in {Seconds} seconds", intervalSeconds);
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("JourneyReminderHostedService: Running journey reminder check.");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var journeyNotificationService = scope.ServiceProvider
                        .GetRequiredService<JourneyNotificationService>();
                    await journeyNotificationService.SendJourneyRemindersAsync(stoppingToken);
                }

                _logger.LogInformation("JourneyReminderHostedService: Journey reminder check completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in JourneyReminderHostedService.");
            }
        }

        _logger.LogInformation("JourneyReminderHostedService stopped.");
    }
}
