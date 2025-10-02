using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	[DisallowConcurrentExecution]
	public class GenerateTaskListJob(IServiceProvider serviceProvider, ILogger<GenerateTaskListJob> logger) : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			Guid jobId = context.MergedJobDataMap.GetGuid("JobId");
			CancellationToken ct = context.CancellationToken;

			using IServiceScope scope = serviceProvider.CreateScope();
			AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			// Prepare phase
			logger.LogInformation("Starting prepare phase for job {JobId}", jobId);
			Job? job = await dbContext.Jobs.FindAsync([jobId], ct);
			if (job == null)
			{
				logger.LogError("Job {JobId} not found", jobId);
				return;
			}

			Job runningJob = job with { Status = JobStatus.Running };
			dbContext.Entry(job).CurrentValues.SetValues(runningJob);
			_ = await dbContext.SaveChangesAsync(ct);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);

			// Execute phase
			logger.LogInformation("Starting execute phase for job {JobId}", jobId);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);
			List<TaskItem> tasks = await dbContext.Tasks.ToListAsync(ct);
			string fileName = $"TaskList_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
			string filePath = Path.Combine(Directory.GetCurrentDirectory(), "TaskLists", fileName);

			_ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			IEnumerable<string> taskList = tasks.Select(t => $"Task: {t.Title}, Status: {t.Status}, Created: {t.CreatedAt}");
			await File.WriteAllLinesAsync(filePath, taskList, ct);

			// Complete phase
			logger.LogInformation("Starting complete phase for job {JobId}", jobId);
			await Task.Delay(TimeSpan.FromMinutes(3), ct);
			Job completedJob = runningJob with { Status = JobStatus.Completed };
			dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
			_ = await dbContext.SaveChangesAsync(ct);
		}
	}
}