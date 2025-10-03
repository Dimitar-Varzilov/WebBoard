using Microsoft.EntityFrameworkCore;
using WebBoard.Common.Cionstants;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services
{
	public class BackgroundService(
		IServiceProvider serviceProvider,
		ILogger<BackgroundService> logger) : IBackgroundService
	{
		public async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ProcessJobsAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error occurred while processing jobs");
				}

				await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
			}
		}

		private async Task ProcessJobsAsync(CancellationToken stoppingToken)
		{
			using var scope = serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var queuedJob = await dbContext.Jobs
				.FirstOrDefaultAsync(j => j.Status == JobStatus.Queued, stoppingToken);

			if (queuedJob == null)
				return;

			// Create new job instance with Running status
			var runningJob = queuedJob with { Status = JobStatus.Running };
			dbContext.Entry(queuedJob).CurrentValues.SetValues(runningJob);
			await dbContext.SaveChangesAsync(stoppingToken);

			try
			{
				switch (queuedJob.JobType)
				{
					case Constants.JobTypes.MarkTasksAsCompleted:
						await MarkAllTasksAsCompletedAsync(dbContext, stoppingToken);
						break;
					case Constants.JobTypes.GenerateTaskList:
						await GenerateTaskListFileAsync(dbContext, stoppingToken);
						break;
				}

				// Create new job instance with Completed status
				var completedJob = runningJob with { Status = JobStatus.Completed };
				dbContext.Entry(runningJob).CurrentValues.SetValues(completedJob);
				await dbContext.SaveChangesAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error processing job {JobId}", queuedJob.Id);
				throw;
			}
		}

		private static async Task MarkAllTasksAsCompletedAsync(AppDbContext dbContext, CancellationToken stoppingToken)
		{
			var pendingTasks = await dbContext.Tasks
				.Where(t => t.Status != TaskItemStatus.Completed)
				.ToListAsync(stoppingToken);

			var updatedTasks = pendingTasks.Select(t => t with { Status = TaskItemStatus.Completed });
			foreach (var (oldTask, newTask) in pendingTasks.Zip(updatedTasks))
			{
				dbContext.Entry(oldTask).CurrentValues.SetValues(newTask);
			}

			await dbContext.SaveChangesAsync(stoppingToken);
		}

		private static async Task GenerateTaskListFileAsync(AppDbContext dbContext, CancellationToken stoppingToken)
		{
			var tasks = await dbContext.Tasks.ToListAsync(stoppingToken);
			var fileName = $"TaskList_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TaskLists", fileName);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			var taskList = tasks.Select(t => $"Task: {t.Title}, Status: {t.Status}, Created: {t.CreatedAt}");
			await File.WriteAllLinesAsync(filePath, taskList, stoppingToken);
		}
	}
}