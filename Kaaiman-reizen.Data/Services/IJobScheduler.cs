namespace Kaaiman_reizen.Services
{
    public interface IJobScheduler
    {
        Task ScheduleJobsForJourney(int journeyId, CancellationToken cancellationToken);
        Task RescheduleJobsForJourney(int journeyId, CancellationToken cancellationToken);
        Task RemoveJobsForJourney(int journeyid, CancellationToken cancellationToken);
    }
}