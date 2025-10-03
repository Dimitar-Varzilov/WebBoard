using Quartz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	public interface IJobCleanupService
	{
		Task CleanupCompletedJobAsync(Guid jobId);
		Task CleanupAllCompletedJobsAsync();
		Task CleanupFromSchedulerOnlyAsync(Guid jobId);
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

				// IMPORTANT: Database removal is controlled by configuration
				// By default, we preserve jobs in database for audit trail
				if (_cleanupOptions.RemoveFromDatabase)
				{
					logger.LogWarning("Database cleanup is enabled - removing job {JobId} from database", jobId);
					cleanupTasks.Add(CleanupFromDatabase(dbContext, job));
				}
				else
				{
					logger.LogInformation("Database cleanup is disabled - preserving job {JobId} in database for audit trail", jobId);
				}

				// Execute cleanup tasks
				await Task.WhenAll(cleanupTasks);

				logger.LogInformation("Successfully cleaned up completed job {JobId} of type {JobType} (Scheduler: {SchedulerCleanup}, Database: {DatabaseCleanup})", 
					jobId, job.JobType, _cleanupOptions.RemoveFromScheduler, _cleanupOptions.RemoveFromDatabase);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error cleaning up job {JobId}", jobId);
				throw;
			}
		}

		public async Task CleanupFromSchedulerOnlyAsync(Guid jobId)
		{
			try
			{
				await CleanupFromScheduler(jobId);
				logger.LogInformation("Removed job {JobId} from scheduler only (database preserved)", jobId);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error cleaning up job {JobId} from scheduler", jobId);
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

				logger.LogInformation("Found {JobCount} completed jobs to cleanup (Scheduler: {SchedulerCleanup}, Database: {DatabaseCleanup})", 
					completedJobs.Count, _cleanupOptions.RemoveFromScheduler, _cleanupOptions.RemoveFromDatabase);

				var schedulerCleanupCount = 0;
				var databaseCleanupCount = 0;
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
								schedulerCleanupCount++;
							}
						}

						// Remove from database if configured (WARNING: This removes audit trail)
						if (_cleanupOptions.RemoveFromDatabase)
						{
							dbContext.Jobs.Remove(job);
							databaseCleanupCount++;
						}
					}
					catch (Exception ex)
					{
						failureCount++;
						logger.LogError(ex, "Failed to cleanup job {JobId} of type {JobType}", job.Id, job.JobType);
					}
				}

				// Save all database changes at once for better performance
				if (_cleanupOptions.RemoveFromDatabase && databaseCleanupCount > 0)
				{
					await dbContext.SaveChangesAsync();
					logger.LogWarning("Removed {DatabaseCleanupCount} completed jobs from database - audit trail lost", databaseCleanupCount);
				}

				logger.LogInformation("Cleanup completed: {SchedulerCleanupCount} jobs removed from scheduler, {DatabaseCleanupCount} jobs removed from database, {FailureCount} failures", 
					schedulerCleanupCount, databaseCleanupCount, failureCount);
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
			logger.LogWarning("Removed job {JobId} from database - audit trail lost for this job", job.Id);
		}
	}
}