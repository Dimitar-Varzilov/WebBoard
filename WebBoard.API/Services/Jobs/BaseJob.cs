using Microsoft.Extensions.Options;
using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	/// <summary>
	/// Base class for jobs with automatic status updates and SignalR notifications
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
			var statusNotifier = scope.ServiceProvider.GetRequiredService<IJobStatusNotifier>();

			try
			{
				// Load the job entity
				var job = await dbContext.Jobs.FindAsync(jobId, ct);
				if (job == null)
				{
					Logger.LogError("Job {JobId} not found", jobId);
					return;
				}

				// Update status to Running and broadcast via SignalR
				await UpdateJobStatusAsync(dbContext, job, JobStatus.Running, statusNotifier, ct);
				Logger.LogInformation("Starting execution of job {JobId} of type {JobType}", jobId, job.JobType);

				// Execute the actual job logic
				await ExecuteJobLogic(dbContext, jobId, ct);

				// Update status to Completed and broadcast via SignalR
				await UpdateJobStatusAsync(dbContext, job, JobStatus.Completed, statusNotifier, ct);
				Logger.LogInformation("Job {JobId} completed successfully", jobId);

				// Clean up from scheduler if configured
				if (cleanupOptions.AutoCleanupCompletedJobs && cleanupOptions.RetentionPeriod == TimeSpan.Zero)
				{
					await jobCleanupService.CleanupFromSchedulerOnlyAsync(jobId);
					Logger.LogInformation("Job {JobId} removed from scheduler but preserved in database for audit trail", jobId);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error processing job {JobId}", jobId);

				// Update job status to Failed and broadcast via SignalR
				try
				{
					var job = await dbContext.Jobs.FindAsync(jobId, ct);
					if (job != null)
					{
						await UpdateJobStatusAsync(dbContext, job, JobStatus.Failed, statusNotifier, ct, ex.Message);
						Logger.LogWarning("Job {JobId} marked as Failed and preserved in database for troubleshooting", jobId);

						// Clean up failed job from scheduler only
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
		/// Update job status in database and notify all clients via SignalR
		/// </summary>
		private async Task UpdateJobStatusAsync(
			AppDbContext dbContext,
			Job job,
			JobStatus newStatus,
			IJobStatusNotifier statusNotifier,
			CancellationToken ct,
			string? errorMessage = null)
		{
			// Update database
			var updatedJob = job with { Status = newStatus };
			dbContext.Entry(job).CurrentValues.SetValues(updatedJob);
			await dbContext.SaveChangesAsync(ct);

			// Broadcast via SignalR to all connected clients
			await statusNotifier.NotifyJobStatusAsync(job.Id, job.JobType, newStatus, errorMessage);

			Logger.LogDebug("Updated job {JobId} status to {Status} and broadcasted via SignalR",
				job.Id, newStatus);
		}

		/// <summary>
		/// Override this method to implement the actual job logic
		/// </summary>
		protected abstract Task ExecuteJobLogic(AppDbContext dbContext, Guid jobId, CancellationToken cancellationToken);
	}
}
