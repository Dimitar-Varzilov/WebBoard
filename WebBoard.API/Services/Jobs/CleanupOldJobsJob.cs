using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.Enums;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Example of how easy it is to add a new job type with the new system
	/// Just add the JobType attribute and inherit from BaseJob
	/// </summary>
	[JobType("CleanupOldJobs")]
	public class CleanupOldJobsJob(IServiceProvider serviceProvider, ILogger<CleanupOldJobsJob> logger)
		: BaseJob(serviceProvider, logger)
	{
		protected override async Task ExecuteJobLogic(AppDbContext dbContext, Guid jobId, CancellationToken cancellationToken)
		{
			Logger.LogInformation("Starting cleanup of old jobs for job {JobId}", jobId);

			// Clean up completed jobs older than 30 days
			var cutoffDate = DateTime.UtcNow.AddDays(-30);
			var oldJobs = await dbContext.Jobs
				.Where(j => j.Status == JobStatus.Completed && j.CreatedAt < cutoffDate)
				.ToListAsync(cancellationToken);

			if (oldJobs.Count > 0)
			{
				dbContext.Jobs.RemoveRange(oldJobs);
				await dbContext.SaveChangesAsync(cancellationToken);
				Logger.LogInformation("Cleaned up {JobCount} old jobs", oldJobs.Count);
			}
			else
			{
				Logger.LogInformation("No old jobs found to clean up");
			}

			Logger.LogInformation("Cleanup of old jobs completed for job {JobId}", jobId);
		}
	}
}