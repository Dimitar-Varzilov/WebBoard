using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	[JobType(Constants.JobTypes.MarkAllTasksAsDone)]
	public class MarkTasksAsCompletedJob(IServiceProvider serviceProvider, ILogger<MarkTasksAsCompletedJob> logger) : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			var jobId = context.MergedJobDataMap.GetGuid(Constants.JobDataKeys.JobId);
			var ct = context.CancellationToken;

			using var scope = serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			try
			{
				// Update job status to Running
				var job = await dbContext.Jobs.FindAsync(jobId, ct);
				if (job == null)
				{
					logger.LogError("Job {JobId} not found", jobId);
					return;
				}

				var runningJob = job with { Status = JobStatus.Running };
				dbContext.Entry(job).CurrentValues.SetValues(runningJob);
				await dbContext.SaveChangesAsync(ct);

				logger.LogInformation("Starting mark tasks as completed for job {JobId}", jobId);

				// Mark all tasks as completed
				await MarkAllTasksAsCompletedAsync(dbContext, ct);

				// Update job status to Completed
				var completedJob = runningJob with { Status = JobStatus.Completed };
				dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
				await dbContext.SaveChangesAsync(ct);

				logger.LogInformation("Mark tasks as completed finished for job {JobId}", jobId);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error processing job {JobId}", jobId);
				throw;
			}
		}

		private static async Task MarkAllTasksAsCompletedAsync(AppDbContext dbContext, CancellationToken ct)
		{
			var pendingTasks = await dbContext.Tasks
				.Where(t => t.Status != TaskItemStatus.Completed)
				.ToListAsync(ct);

			var updatedTasks = pendingTasks.Select(t => t with { Status = TaskItemStatus.Completed });
			foreach (var (oldTask, newTask) in pendingTasks.Zip(updatedTasks))
			{
				dbContext.Entry(oldTask).CurrentValues.SetValues(newTask);
			}

			await dbContext.SaveChangesAsync(ct);
		}
	}
}