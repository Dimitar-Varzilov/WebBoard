using Microsoft.Extensions.Options;
using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	/// <summary>
	/// Base class for jobs that automatically handles status updates and cleanup
	/// Jobs are NEVER removed from database to maintain complete audit trail
	/// </summary>
	public abstract class BaseJob(IServiceProvider serviceProvider, ILogger logger) : IJob
	{
        protected readonly IServiceProvider ServiceProvider = serviceProvider;
        protected readonly ILogger Logger = logger;

		public async Task Execute(IJobExecutionContext context)
		{
			var jobId = context.MergedJobDataMap.GetGuid(Constants.JobDataKeys.JobId);
			var ct = context.CancellationToken;

			using var scope = ServiceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var jobCleanupService = scope.ServiceProvider.GetRequiredService<IJobCleanupService>();
			var cleanupOptions = scope.ServiceProvider.GetRequiredService<IOptions<JobCleanupOptions>>().Value;

			try
			{
				// Load the job entity (this will be tracked by EF)
				var job = await dbContext.Jobs.FindAsync(jobId, ct);
				if (job == null)
				{
					Logger.LogError("Job {JobId} not found", jobId);
					return;
				}

				// Update job status to Running
				await UpdateJobStatus(dbContext, job, JobStatus.Running, ct);
				Logger.LogInformation("Starting execution of job {JobId} of type {JobType}", jobId, job.JobType);

				// Execute the actual job logic
				await ExecuteJobLogic(dbContext, jobId, ct);

				// Update job status to Completed
				await UpdateJobStatus(dbContext, job, JobStatus.Completed, ct);
				Logger.LogInformation("Job {JobId} completed successfully", jobId);

				// Clean up the completed job from scheduler only (NEVER from database)
				if (cleanupOptions.AutoCleanupCompletedJobs)
				{
					if (cleanupOptions.RetentionPeriod == TimeSpan.Zero)
					{
						// Immediate cleanup from scheduler only
						await jobCleanupService.CleanupFromSchedulerOnlyAsync(jobId);
						Logger.LogInformation("Job {JobId} removed from scheduler but preserved in database for audit trail", jobId);
					}
					else
					{
						// Schedule cleanup after retention period (you could implement this with another job)
						Logger.LogInformation("Job {JobId} will be cleaned up from scheduler after retention period of {RetentionPeriod}",
							jobId, cleanupOptions.RetentionPeriod);
					}
				}
				else
				{
					Logger.LogDebug("Auto cleanup is disabled, job {JobId} will remain in both scheduler and database", jobId);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error processing job {JobId}", jobId);

				// Update job status to Failed and preserve in database for troubleshooting
				try
				{
					var job = await dbContext.Jobs.FindAsync(jobId, ct);
					if (job != null)
					{
						await UpdateJobStatus(dbContext, job, JobStatus.Failed, ct);
						Logger.LogWarning("Job {JobId} marked as Failed and preserved in database for troubleshooting", jobId);

						// Clean up failed job from scheduler only (keep in database for audit)
						try
						{
							await jobCleanupService.CleanupFromSchedulerOnlyAsync(jobId);
							Logger.LogInformation("Failed job {JobId} removed from scheduler but preserved in database", jobId);
						}
						catch (Exception cleanupEx)
						{
							Logger.LogError(cleanupEx, "Failed to cleanup job {JobId} from scheduler after failure", jobId);
						}
					}
				}
				catch (Exception statusUpdateEx)
				{
					Logger.LogError(statusUpdateEx, "Failed to update job status to Failed for job {JobId}", jobId);
				}

				throw;
			}
		}

		/// <summary>
		/// Helper method to update job status using the tracked entity
		/// Jobs are always preserved in database regardless of status
		/// </summary>
		private async Task UpdateJobStatus(AppDbContext dbContext, Job job, JobStatus newStatus, CancellationToken ct)
		{
			var updatedJob = job with { Status = newStatus };
			dbContext.Entry(job).CurrentValues.SetValues(updatedJob);
			var rowsUpdated = await dbContext.SaveChangesAsync(ct);

			Logger.LogDebug("Updated job {JobId} status to {Status}, {RowsUpdated} rows affected (preserved in database)",
				job.Id, newStatus, rowsUpdated);
		}

		/// <summary>
		/// Override this method to implement the actual job logic
		/// </summary>
		/// <param name="dbContext">Database context</param>
		/// <param name="jobId">Job ID</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Task representing the async operation</returns>
		protected abstract Task ExecuteJobLogic(AppDbContext dbContext, Guid jobId, CancellationToken cancellationToken);
	}
}