using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	public interface IJobCleanupService
	{
		Task CleanupCompletedJobAsync(Guid jobId);
		Task CleanupAllCompletedJobsAsync();
	}

	public class JobCleanupService(
		IScheduler scheduler,
		IServiceProvider serviceProvider,
		IOptions<JobCleanupOptions> cleanupOptions,
		ILogger<JobCleanupService> logger) : IJobCleanupService
	{
		private readonly JobCleanupOptions _cleanupOptions = cleanupOptions.Value;

		public async Task CleanupCompletedJobAsync(Guid jobId)
		{
			try
			{
				using var scope = serviceProvider.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				// Get the job from database
				var job = await dbContext.Jobs.FindAsync(jobId);
				if (job == null)
				{
					logger.LogWarning("Job {JobId} not found in database during cleanup", jobId);
					return;
				}

				// Only cleanup completed jobs
				if (job.Status != JobStatus.Completed)
				{
					logger.LogWarning("Job {JobId} is not completed (Status: {Status}), skipping cleanup", jobId, job.Status);
					return;
				}

				var cleanupTasks = new List<Task>();

				// Remove from Quartz scheduler if configured
				if (_cleanupOptions.RemoveFromScheduler)
				{
					cleanupTasks.Add(CleanupFromScheduler(jobId));
				}

				// Remove from database if configured
				if (_cleanupOptions.RemoveFromDatabase)
				{
					cleanupTasks.Add(CleanupFromDatabase(dbContext, job));
				}

				// Execute cleanup tasks
				await Task.WhenAll(cleanupTasks);

				logger.LogInformation("Successfully cleaned up completed job {JobId} of type {JobType}", jobId, job.JobType);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error cleaning up job {JobId}", jobId);
				throw;
			}
		}

		public async Task CleanupAllCompletedJobsAsync()
		{
			try
			{
				using var scope = serviceProvider.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				// Get all completed jobs
				var completedJobs = await dbContext.Jobs
					.Where(j => j.Status == JobStatus.Completed)
					.ToListAsync();

				if (completedJobs.Count == 0)
				{
					logger.LogInformation("No completed jobs found to cleanup");
					return;
				}

				logger.LogInformation("Found {JobCount} completed jobs to cleanup", completedJobs.Count);

				var successCount = 0;
				var failureCount = 0;

				foreach (var job in completedJobs)
				{
					try
					{
						// Remove from Quartz scheduler if configured
						if (_cleanupOptions.RemoveFromScheduler)
						{
							var jobKey = new JobKey(job.Id.ToString());
							if (await scheduler.CheckExists(jobKey))
							{
								await scheduler.DeleteJob(jobKey);
							}
						}

						// Remove from database if configured
						if (_cleanupOptions.RemoveFromDatabase)
						{
							dbContext.Jobs.Remove(job);
						}

						successCount++;
					}
					catch (Exception ex)
					{
						failureCount++;
						logger.LogError(ex, "Failed to cleanup job {JobId} of type {JobType}", job.Id, job.JobType);
					}
				}

				// Save all changes at once for better performance
				if (_cleanupOptions.RemoveFromDatabase && successCount > 0)
				{
					await dbContext.SaveChangesAsync();
				}

				logger.LogInformation("Cleanup completed: {SuccessCount} jobs cleaned up, {FailureCount} failures", successCount, failureCount);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during bulk cleanup of completed jobs");
				throw;
			}
		}

		private async Task CleanupFromScheduler(Guid jobId)
		{
			var jobKey = new JobKey(jobId.ToString());
			if (await scheduler.CheckExists(jobKey))
			{
				var deleted = await scheduler.DeleteJob(jobKey);
				if (deleted)
				{
					logger.LogInformation("Removed job {JobId} from Quartz scheduler", jobId);
				}
				else
				{
					logger.LogWarning("Failed to remove job {JobId} from Quartz scheduler", jobId);
				}
			}
			else
			{
				logger.LogDebug("Job {JobId} not found in Quartz scheduler (already removed or never scheduled)", jobId);
			}
		}

		private async Task CleanupFromDatabase(AppDbContext dbContext, Common.Models.Job job)
		{
			dbContext.Jobs.Remove(job);
			await dbContext.SaveChangesAsync();
			logger.LogInformation("Removed job {JobId} from database", job.Id);
		}
	}
}