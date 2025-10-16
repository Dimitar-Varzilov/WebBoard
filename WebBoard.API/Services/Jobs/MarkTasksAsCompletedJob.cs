using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Enums;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Jobs
{
	[JobType(Constants.JobTypes.MarkAllTasksAsDone)]
	public class MarkTasksAsCompletedJob(IServiceProvider serviceProvider, ILogger<MarkTasksAsCompletedJob> logger)
		: BaseJob(serviceProvider, logger)
	{
		protected override async Task<JobExecutionResult> ExecuteJobLogic(
			IServiceProvider scopedServices,
			AppDbContext dbContext,
			Guid jobId,
			CancellationToken cancellationToken)
		{
			Logger.LogInformation("Starting mark tasks as completed for job {JobId}", jobId);

			try
			{
				// Mark only tasks assigned to this job as completed
				var updatedCount = await MarkJobTasksAsCompletedAsync(dbContext, jobId, cancellationToken);

				Logger.LogInformation("Marked {TaskCount} tasks as completed for job {JobId}", updatedCount, jobId);

				// Return success with the number of tasks processed
				return new JobExecutionResult(
					IsSuccess: true,
					TasksProcessed: updatedCount);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error marking tasks as completed for job {JobId}", jobId);

				// Return failure with error message
				return new JobExecutionResult(
					IsSuccess: false,
					TasksProcessed: 0,
					ErrorMessage: ex.Message);
			}
		}

		/// <summary>
		/// Marks all pending tasks assigned to the specified job as completed
		/// </summary>
		/// <param name="dbContext">Database context</param>
		/// <param name="jobId">The job ID to process tasks for</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>Number of tasks updated</returns>
		private async Task<int> MarkJobTasksAsCompletedAsync(AppDbContext dbContext, Guid jobId, CancellationToken ct)
		{
			// Get only pending tasks assigned to this specific job
			var pendingTasks = await dbContext.Tasks
				.Where(t => t.JobId == jobId && t.Status == TaskItemStatus.Pending)
				.ToListAsync(ct);

			if (pendingTasks.Count == 0)
			{
				Logger.LogWarning("No pending tasks found for job {JobId}", jobId);
				return 0;
			}

			Logger.LogInformation("Found {TaskCount} pending tasks for job {JobId}", pendingTasks.Count, jobId);

			// Update tasks to completed status
			var updatedTasks = pendingTasks.Select(t => t with { Status = TaskItemStatus.Completed });
			foreach (var (oldTask, newTask) in pendingTasks.Zip(updatedTasks))
			{
				dbContext.Entry(oldTask).CurrentValues.SetValues(newTask);
			}

			await dbContext.SaveChangesAsync(ct);

			return pendingTasks.Count;
		}
	}
}