using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Common;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	[DisallowConcurrentExecution]
	public class MarkTasksAsCompletedJob(IServiceProvider serviceProvider, ILogger<MarkTasksAsCompletedJob> logger) : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			var jobId = context.MergedJobDataMap.GetGuid(Constants.JobDataKeys.JobId);
			var ct = context.CancellationToken;

			using var scope = serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			// Prepare phase
			logger.LogInformation("Starting prepare phase for job {JobId}", jobId);
			var job = await dbContext.Jobs.FindAsync(jobId, ct);
			if (job == null)
			{
				logger.LogError("Job {JobId} not found", jobId);
				return;
			}

			var runningJob = job with { Status = JobStatus.Running };
			dbContext.Entry(job).CurrentValues.SetValues(runningJob);
			await dbContext.SaveChangesAsync(ct);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);

			// Execute phase
			logger.LogInformation("Starting execute phase for job {JobId}", jobId);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);
			var pendingTasks = await dbContext.Tasks
				.Where(t => t.Status != TaskItemStatus.Completed)
				.ToListAsync(ct);

			foreach (var task in pendingTasks)
			{
				var updatedTask = task with { Status = TaskItemStatus.Completed };
				dbContext.Entry(task).CurrentValues.SetValues(updatedTask);
			}
			await dbContext.SaveChangesAsync(ct);

			// Complete phase
			logger.LogInformation("Starting complete phase for job {JobId}", jobId);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);
			var completedJob = runningJob with { Status = JobStatus.Completed };
			dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
			await dbContext.SaveChangesAsync(ct);
		}
	}
}