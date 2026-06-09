using Hangfire;
using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;

namespace Kaaiman_reizen.Services
{
    public class JobScheduler : IJobScheduler
    {
        private readonly MainContext _db;
        private readonly ITravelLeaderService _travelLeaderService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public JobScheduler(MainContext db, ITravelLeaderService travelLeaderService, IBackgroundJobClient backgroundJobClient) {
            _db = db;
            _travelLeaderService = travelLeaderService;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task ScheduleJobsForJourney(int journeyId, CancellationToken cancellationToken)
        {
            var journey = await _db.Journey.FindAsync(new object[] { journeyId }, cancellationToken);

            if (journey is null)
                return; 

            var executionTime = journey.End.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc);

            var nieuwJobId = _backgroundJobClient.Schedule<JobScheduler>(
                service => service.HandleJourneyConclusion(journey.Id, CancellationToken.None),
                executionTime
            );

            journey.HangfireJobId = nieuwJobId;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RescheduleJobsForJourney(int journeyId, CancellationToken cancellationToken)
        {
            await RemoveJobsForJourney(journeyId, cancellationToken);
            await ScheduleJobsForJourney(journeyId, cancellationToken);
        }

        public async Task RemoveJobsForJourney(int journeyId, CancellationToken cancellationToken)
        {
            var journey = await _db.Journey.FindAsync(new object[] { journeyId }, cancellationToken);

            if (journey is null || string.IsNullOrEmpty(journey.HangfireJobId))
                return;

            _backgroundJobClient.Delete(journey.HangfireJobId);

            journey.HangfireJobId = null;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task HandleJourneyConclusion(int journeyId, CancellationToken cancellationToken)
        {
            var journey = await _db.Journey.FindAsync(new object[] { journeyId }, cancellationToken);

            if (journey is not null)
            {
                await _travelLeaderService.IncrementTravelLeadersExperience(journeyId, cancellationToken);

                journey.BookingStatus = BookingStatus.Geweest;
                journey.IsProcessed = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}