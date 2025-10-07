namespace WebBoard.Services.Jobs
{
	public interface IJobCleanupService
	{
		Task CleanupCompletedJobAsync(Guid jobId);
		Task CleanupAllCompletedJobsAsync();
		Task CleanupFromSchedulerOnlyAsync(Guid jobId);
	}
}