using Microsoft.EntityFrameworkCore;
using WebBoard.Common.Constants;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	[JobType(Constants.JobTypes.GenerateTaskReport)]
	public class GenerateTaskListJob(IServiceProvider serviceProvider, ILogger<GenerateTaskListJob> logger)
		: BaseJob(serviceProvider, logger)
	{
		protected override async Task ExecuteJobLogic(AppDbContext dbContext, Guid jobId, CancellationToken cancellationToken)
		{
			Logger.LogInformation("Starting task list generation for job {JobId}", jobId);

			// Generate task list file
			await GenerateTaskListFileAsync(dbContext, cancellationToken);

			Logger.LogInformation("Task list generation completed for job {JobId}", jobId);
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