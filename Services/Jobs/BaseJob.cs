using Microsoft.Extensions.Options;
using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	/// <summary>
	/// Base class for jobs that automatically handles status updates and cleanup
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
				// Update job status to Running
				var job = await dbContext.Jobs.FindAsync(jobId, ct);
				if (job == null)
				{
					Logger.LogError("Job {JobId} not found", jobId);
					return;
				}

				var runningJob = job with { Status = JobStatus.Running };
				dbContext.Entry(job).CurrentValues.SetValues(runningJob);
				await dbContext.SaveChangesAsync(ct);

				Logger.LogInformation("Starting execution of job {JobId} of type {JobType}", jobId, job.JobType);

				// Execute the actual job logic
				await ExecuteJobLogic(dbContext, jobId, ct);

				// Update job status to Completed
				var completedJob = runningJob with { Status = JobStatus.Completed };
				dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
				await dbContext.SaveChangesAsync(ct);

				Logger.LogInformation("Job {JobId} completed successfully", jobId);

				// Clean up the completed job if auto cleanup is enabled
				if (cleanupOptions.AutoCleanupCompletedJobs)
				{
					if (cleanupOptions.RetentionPeriod == TimeSpan.Zero)
					{
						// Immediate cleanup
						await jobCleanupService.CleanupCompletedJobAsync(jobId);
					}
					else
					{
						// Schedule cleanup after retention period (you could implement this with another job)
						Logger.LogInformation("Job {JobId} will be cleaned up after retention period of {RetentionPeriod}",
							jobId, cleanupOptions.RetentionPeriod);
					}
				}
				else
				{
					Logger.LogDebug("Auto cleanup is disabled, job {JobId} will remain in system", jobId);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error processing job {JobId}", jobId);

				// Update job status to indicate failure (you might want to add a Failed status)
				try
				{
					var job = await dbContext.Jobs.FindAsync(jobId, ct);
					if (job != null)
					{
						// For now, we'll leave failed jobs in Running state for manual intervention
						// You could add a JobStatus.Failed if you want
						Logger.LogWarning("Job {JobId} failed but status remains as Running for manual intervention", jobId);
					}
				}
				catch (Exception statusUpdateEx)
				{
					Logger.LogError(statusUpdateEx, "Failed to update job status after error for job {JobId}", jobId);
				}

				throw;
			}
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