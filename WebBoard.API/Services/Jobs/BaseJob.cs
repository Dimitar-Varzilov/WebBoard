using Microsoft.EntityFrameworkCore;
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
	/// Base class for jobs with automatic status updates, SignalR notifications, and retry logic
	/// </summary>
	public abstract class BaseJob(IServiceProvider serviceProvider, ILogger logger) : IJob
	{
		protected readonly IServiceProvider ServiceProvider = serviceProvider;
		protected readonly ILogger Logger = logger;

		/// <summary>
		/// Gets the maximum number of retry attempts for this job type
		/// Override in derived classes to customize retry behavior
		/// </summary>
		protected virtual int MaxRetryAttempts => 3;

		/// <summary>
		/// Determines if a specific error should trigger a retry
		/// Override to customize retry logic per job type
		/// </summary>
		protected virtual bool ShouldRetryOnError(string? errorMessage)
		{
			// Don't retry validation errors
			if (errorMessage?.Contains("validation", StringComparison.OrdinalIgnoreCase) == true)
				return false;

			// Don't retry "not found" errors
			if (errorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
				return false;

			// Retry all other errors
			return true;
		}

		public async Task Execute(IJobExecutionContext context)
		{
			var jobId = context.MergedJobDataMap.GetGuid(Constants.JobDataKeys.JobId);
			var ct = context.CancellationToken;
			var executionStartTime = DateTimeOffset.UtcNow;

			using var scope = ServiceProvider.CreateScope();
			var scopedServices = scope.ServiceProvider;
			var dbContext = scopedServices.GetRequiredService<AppDbContext>();
			var jobCleanupService = scopedServices.GetRequiredService<IJobCleanupService>();
			var statusNotifier = scopedServices.GetRequiredService<IJobStatusNotifier>();
			var retryService = scopedServices.GetRequiredService<IJobRetryService>();

			// Load the job entity
			var job = await dbContext.Jobs.FindAsync(jobId, ct);
			if (job == null)
			{
				Logger.LogError("Job {JobId} not found", jobId);
				return;
			}

			// Get retry information
			var retryInfo = await retryService.GetRetryInfoAsync(jobId);
			var attemptNumber = (retryInfo?.RetryCount ?? 0) + 1;

			// Create structured logging scope with job context
			using (Logger.BeginScope(new Dictionary<string, object>
			{
				["JobId"] = jobId,
				["JobType"] = job.JobType,
				["AttemptNumber"] = attemptNumber,
				["MaxAttempts"] = MaxRetryAttempts,
				["IsRetry"] = retryInfo != null,
				["PreviousError"] = retryInfo?.LastErrorMessage ?? "N/A"
			}))
			{
				JobExecutionResult result;

				try
				{
					// Update status to Running and broadcast via SignalR
					await UpdateJobStatusAsync(dbContext, job, JobStatus.Running, statusNotifier, ct);

					Logger.LogInformation(
						"Job execution started - JobId: {JobId}, Type: {JobType}, Attempt: {Attempt}/{Max}, IsRetry: {IsRetry}",
						jobId, job.JobType, attemptNumber, MaxRetryAttempts, retryInfo != null);

					// Execute the actual job logic - pass scoped service provider to avoid nested scopes
					result = await ExecuteJobLogic(scopedServices, dbContext, jobId, ct);

					var executionDuration = DateTimeOffset.UtcNow - executionStartTime;

					if (result.IsSuccess)
					{
						// Success - remove retry tracking
						await retryService.RemoveRetryInfoAsync(jobId);

						// Update status to Completed and broadcast via SignalR
						await UpdateJobStatusAsync(dbContext, job, JobStatus.Completed, statusNotifier, ct);

						Logger.LogInformation(
							"Job completed successfully - JobId: {JobId}, Type: {JobType}, Attempt: {Attempt}/{Max}, " +
							"TasksProcessed: {TaskCount}, Duration: {Duration}ms, TotalAttempts: {TotalAttempts}",
							jobId, job.JobType, attemptNumber, MaxRetryAttempts,
							result.TasksProcessed, executionDuration.TotalMilliseconds, attemptNumber);

						// Update tasks to completed status if job completed successfully
						await UpdateJobTasksOnCompletionAsync(dbContext, jobId, ct);
					}
					else
					{
						// Job returned failure - check if we should retry
						Logger.LogWarning(
							"Job execution failed - JobId: {JobId}, Type: {JobType}, Attempt: {Attempt}/{Max}, " +
							"Error: {Error}, Duration: {Duration}ms, WillRetry: {WillRetry}",
							jobId, job.JobType, attemptNumber, MaxRetryAttempts,
							result.ErrorMessage, executionDuration.TotalMilliseconds,
							await retryService.ShouldRetryJobAsync(jobId));

						await HandleJobFailureAsync(job, retryService, dbContext, statusNotifier,
							result.ErrorMessage, ct);
					}
				}
				catch (Exception ex)
				{
					var executionDuration = DateTimeOffset.UtcNow - executionStartTime;

					Logger.LogError(ex,
						"Job execution exception - JobId: {JobId}, Type: {JobType}, Attempt: {Attempt}/{Max}, " +
						"Duration: {Duration}ms, ExceptionType: {ExceptionType}",
						jobId, job.JobType, attemptNumber, MaxRetryAttempts,
						executionDuration.TotalMilliseconds, ex.GetType().Name);

					// Handle exception with retry logic
					await HandleJobFailureAsync(job, retryService, dbContext, statusNotifier,
						ex.Message, ct);

					// Check if we should retry
					var shouldRetry = await retryService.ShouldRetryJobAsync(jobId);
					if (!shouldRetry)
					{
						Logger.LogError(
							"Job failed permanently - JobId: {JobId}, Type: {JobType}, TotalAttempts: {TotalAttempts}, " +
							"FinalError: {Error}",
							jobId, job.JobType, attemptNumber, ex.Message);

						// Final failure - rethrow
						throw;
					}

					// Will retry - don't rethrow
					Logger.LogInformation(
						"Job retry scheduled - JobId: {JobId}, NextAttempt: {NextAttempt}/{Max}",
						jobId, attemptNumber + 1, MaxRetryAttempts);
				}
				finally
				{
					// Cleanup completed jobs based on configuration
					await jobCleanupService.CleanupCompletedJobAsync(jobId);
				}
			}
		}

		/// <summary>
		/// Handles job failure and schedules retry if appropriate
		/// </summary>
		private async Task HandleJobFailureAsync(
			Job job,
			IJobRetryService retryService,
			AppDbContext dbContext,
			IJobStatusNotifier statusNotifier,
			string? errorMessage,
			CancellationToken ct)
		{
			var shouldRetry = ShouldRetryOnError(errorMessage) &&
				await retryService.ShouldRetryJobAsync(job.Id);

			if (shouldRetry)
			{
				// Schedule retry
				await retryService.ScheduleRetryAsync(job.Id, errorMessage ?? "Unknown error");

				var retryInfo = await retryService.GetRetryInfoAsync(job.Id);
				var nextAttempt = (retryInfo?.RetryCount ?? 0) + 1;

				Logger.LogWarning(
					"Retry scheduled - JobId: {JobId}, NextAttempt: {NextAttempt}/{Max}, " +
					"ScheduledFor: {RetryTime}, Delay: {DelayMinutes}min, Reason: {Reason}",
					job.Id, nextAttempt, MaxRetryAttempts, retryInfo?.NextRetryAt,
					(retryInfo?.NextRetryAt - DateTimeOffset.UtcNow)?.TotalMinutes ?? 0,
					errorMessage);

				// Update status to show retry is scheduled
				await UpdateJobStatusAsync(dbContext, job, JobStatus.Running, statusNotifier, ct,
					$"Retry scheduled (Attempt {nextAttempt}/{MaxRetryAttempts}): {errorMessage}");
			}
			else
			{
				// Final failure - no more retries
				var retryInfo = await retryService.GetRetryInfoAsync(job.Id);
				var totalAttempts = (retryInfo?.RetryCount ?? 0) + 1;

				Logger.LogError(
					"Job retry limit reached - JobId: {JobId}, Type: {JobType}, TotalAttempts: {TotalAttempts}, " +
					"FinalError: {Error}, RetrySkipped: {RetrySkipped}",
					job.Id, job.JobType, totalAttempts, errorMessage, !ShouldRetryOnError(errorMessage));

				await UpdateJobStatusAsync(dbContext, job, JobStatus.Failed, statusNotifier, ct,
					$"Failed after {totalAttempts} attempt(s): {errorMessage}");

				// Remove retry tracking
				await retryService.RemoveRetryInfoAsync(job.Id);
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
