using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Common.Cionstants;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	[DisallowConcurrentExecution]
	public class GenerateTaskListJob(IServiceProvider serviceProvider, ILogger<GenerateTaskListJob> logger) : IJob
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
			var tasks = await dbContext.Tasks.ToListAsync(ct);
			var fileName = $"TaskList_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TaskLists", fileName);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			var taskList = tasks.Select(t => $"Task: {t.Title}, Status: {t.Status}, Created: {t.CreatedAt}");
			await File.WriteAllLinesAsync(filePath, taskList, ct);

			// Complete phase
			logger.LogInformation("Starting complete phase for job {JobId}", jobId);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);
			var completedJob = runningJob with { Status = JobStatus.Completed };
			dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
			await dbContext.SaveChangesAsync(ct);
		}
	}
}