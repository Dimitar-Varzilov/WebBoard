using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Represents the result of job execution
	/// </summary>
	public record JobExecutionResult(
		bool IsSuccess,
		int TasksProcessed = 0,
		string? ErrorMessage = null);

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
			var scopedServices = scope.ServiceProvider;
			var dbContext = scopedServices.GetRequiredService<AppDbContext>();
			var jobCleanupService = scopedServices.GetRequiredService<IJobCleanupService>();
			var statusNotifier = scopedServices.GetRequiredService<IJobStatusNotifier>();

			// Load the job entity
			var job = await dbContext.Jobs.FindAsync(jobId, ct);
			if (job == null)
			{
				Logger.LogError("Job {JobId} not found", jobId);
				return;
			}

			JobExecutionResult result;

			try
			{
				// Update status to Running and broadcast via SignalR
				await UpdateJobStatusAsync(dbContext, job, JobStatus.Running, statusNotifier, ct);
				Logger.LogInformation("Starting execution of job {JobId} of type {JobType}", jobId, job.JobType);

				// Execute the actual job logic - pass scoped service provider to avoid nested scopes
				result = await ExecuteJobLogic(scopedServices, dbContext, jobId, ct);

				if (result.IsSuccess)
				{
					// Update status to Completed and broadcast via SignalR
					await UpdateJobStatusAsync(dbContext, job, JobStatus.Completed, statusNotifier, ct);
					Logger.LogInformation("Job {JobId} completed successfully. Processed {TaskCount} tasks", 
						jobId, result.TasksProcessed);

					// Update tasks to completed status if job completed successfully
					await UpdateJobTasksOnCompletionAsync(dbContext, jobId, ct);
				}
				else
				{
					// Job returned failure
					await UpdateJobStatusAsync(dbContext, job, JobStatus.Failed, statusNotifier, ct, result.ErrorMessage);
					Logger.LogWarning("Job {JobId} completed with failure: {ErrorMessage}", jobId, result.ErrorMessage);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error processing job {JobId}", jobId);

				// Update job status to Failed and broadcast via SignalR
				try
				{
					await UpdateJobStatusAsync(dbContext, job, JobStatus.Failed, statusNotifier, ct, ex.Message);
					Logger.LogWarning("Job {JobId} marked as Failed and preserved in database for troubleshooting", jobId);
				}
				catch (Exception statusUpdateEx)
				{
					Logger.LogError(statusUpdateEx, "Failed to update job status to Failed for job {JobId}", jobId);
				}

				throw;
			}
			finally
			{
				// Cleanup completed jobs based on configuration
				await jobCleanupService.CleanupCompletedJobAsync(jobId);
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
		/// Updates all tasks assigned to the job to Completed status when job completes successfully
		/// This is a common operation that runs after successful job completion
		/// </summary>
		/// <param name="dbContext">Database context</param>
		/// <param name="jobId">The job ID whose tasks should be updated</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>Number of tasks updated</returns>
		protected async Task<int> UpdateJobTasksOnCompletionAsync(
			AppDbContext dbContext,
			Guid jobId,
			CancellationToken ct)
		{
			// Get all pending or in-progress tasks assigned to this job
			var tasksToUpdate = await dbContext.Tasks
				.Where(t => t.JobId == jobId && 
					(t.Status == TaskItemStatus.Pending || t.Status == TaskItemStatus.InProgress))
				.ToListAsync(ct);

			if (tasksToUpdate.Count == 0)
			{
				Logger.LogInformation("No tasks to update for completed job {JobId}", jobId);
				return 0;
			}

			// Update all tasks to Completed status
			var updatedTasks = tasksToUpdate.Select(t => t with { Status = TaskItemStatus.Completed });
			foreach (var (oldTask, newTask) in tasksToUpdate.Zip(updatedTasks))
			{
				dbContext.Entry(oldTask).CurrentValues.SetValues(newTask);
			}

			await dbContext.SaveChangesAsync(ct);

			Logger.LogInformation("Updated {TaskCount} tasks to Completed status for job {JobId}", 
				tasksToUpdate.Count, jobId);

			return tasksToUpdate.Count;
		}

		/// <summary>
		/// Override this method to implement the actual job logic
		/// </summary>
		/// <param name="scopedServices">Scoped service provider for resolving services without creating nested scopes</param>
		/// <param name="dbContext">Database context from the current scope</param>
		/// <param name="jobId">The job ID being executed</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>JobExecutionResult indicating success/failure and number of tasks processed</returns>
		protected abstract Task<JobExecutionResult> ExecuteJobLogic(
			IServiceProvider scopedServices,
			AppDbContext dbContext,
			Guid jobId,
			CancellationToken cancellationToken);
	}
}
