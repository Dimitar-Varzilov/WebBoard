using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	public class GenerateTaskListJob(IServiceProvider serviceProvider, ILogger<GenerateTaskListJob> logger) : IJob
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

				logger.LogInformation("Starting task list generation for job {JobId}", jobId);

				// Generate task list file
				await GenerateTaskListFileAsync(dbContext, ct);

				// Update job status to Completed
				var completedJob = runningJob with { Status = JobStatus.Completed };
				dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
				await dbContext.SaveChangesAsync(ct);

				logger.LogInformation("Task list generation completed for job {JobId}", jobId);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error processing job {JobId}", jobId);
				throw;
			}
		}

		private static async Task GenerateTaskListFileAsync(AppDbContext dbContext, CancellationToken ct)
		{
			var tasks = await dbContext.Tasks.ToListAsync(ct);
			var fileName = $"TaskList_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TaskLists", fileName);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			var taskList = tasks.Select(t => $"Task: {t.Title}, Status: {t.Status}, Created: {t.CreatedAt}");
			await File.WriteAllLinesAsync(filePath, taskList, ct);
		}
	}
}