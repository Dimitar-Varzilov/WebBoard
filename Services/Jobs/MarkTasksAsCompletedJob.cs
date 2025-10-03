using Microsoft.EntityFrameworkCore;
using WebBoard.Common.Constants;
using WebBoard.Common.Enums;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	[JobType(Constants.JobTypes.MarkAllTasksAsDone)]
	public class MarkTasksAsCompletedJob(IServiceProvider serviceProvider, ILogger<MarkTasksAsCompletedJob> logger)
		: BaseJob(serviceProvider, logger)
	{
		protected override async Task ExecuteJobLogic(AppDbContext dbContext, Guid jobId, CancellationToken cancellationToken)
		{
			Logger.LogInformation("Starting mark tasks as completed for job {JobId}", jobId);

			// Mark all tasks as completed
			await MarkAllTasksAsCompletedAsync(dbContext, cancellationToken);

			Logger.LogInformation("Mark tasks as completed finished for job {JobId}", jobId);
		}

		private static async Task MarkAllTasksAsCompletedAsync(AppDbContext dbContext, CancellationToken ct)
		{
			var pendingTasks = await dbContext.Tasks
				.Where(t => t.Status == TaskItemStatus.Pending)
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