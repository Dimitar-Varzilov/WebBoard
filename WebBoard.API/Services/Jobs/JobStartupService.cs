using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.API.Common.Enums;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Jobs
{
	public class JobStartupService(
		IServiceProvider serviceProvider,
		IScheduler scheduler,
		ILogger<JobStartupService> logger) : IHostedService
	{
		private bool _hasRunOnce = false;
		private readonly Lock _lock = new();

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			try
			{
				// Ensure this only runs once per application lifetime
				lock (_lock)
				{
					if (_hasRunOnce)
					{
						logger.LogInformation("Job Startup Service has already run, skipping");
						return;
					}
					_hasRunOnce = true;
				}

				logger.LogInformation("Job Startup Service is starting - checking for pending jobs");

				// Wait a moment to ensure Quartz scheduler is fully initialized
				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

				// Ensure scheduler is started
				if (!scheduler.IsStarted)
				{
					logger.LogInformation("Waiting for Quartz scheduler to start...");
					var maxWait = TimeSpan.FromSeconds(30);
					var startTime = DateTime.UtcNow;

					while (!scheduler.IsStarted && DateTime.UtcNow - startTime < maxWait)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
					}

					if (!scheduler.IsStarted)
					{
						logger.LogWarning("Quartz scheduler is not started after waiting {MaxWait} seconds, proceeding anyway", maxWait.TotalSeconds);
					}
				}

				// Create scope for scoped services (DbContext and JobSchedulingService)
				using var scope = serviceProvider.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
				var jobSchedulingService = scope.ServiceProvider.GetRequiredService<IJobSchedulingService>();

				// Find all queued jobs that haven't been scheduled yet
				// Use a timestamp check to avoid interfering with jobs created during startup
				var startupTime = DateTime.UtcNow.AddMinutes(-1); // Only consider jobs created before startup
				var pendingJobs = await dbContext.Jobs
					.Where(j => j.Status == JobStatus.Queued && j.CreatedAt < startupTime)
					.OrderBy(j => j.ScheduledAt ?? j.CreatedAt) // Prioritize by scheduled time, then creation time
					.ToListAsync(cancellationToken);

				if (pendingJobs.Count == 0)
				{
					logger.LogInformation("No pending jobs found on startup");
					return;
				}

				logger.LogInformation("Found {JobCount} pending jobs on startup, scheduling them now", pendingJobs.Count);

				var scheduledCount = 0;
				var failedCount = 0;

				foreach (var job in pendingJobs)
				{
					try
					{
						// Check if the job should have already run (scheduled time has passed)
						if (job.ScheduledAt.HasValue && job.ScheduledAt.Value <= DateTime.UtcNow)
						{
							logger.LogInformation("Job {JobId} was scheduled for {ScheduledTime} but is overdue, scheduling to run immediately",
								job.Id, job.ScheduledAt.Value);

							// Create a copy with no scheduled time to run immediately
							var immediateJob = job with { ScheduledAt = null };
							await jobSchedulingService.ScheduleJobAsync(immediateJob);
						}
						else
						{
							// Schedule normally (either immediate or future)
							await jobSchedulingService.ScheduleJobAsync(job);
						}

						scheduledCount++;
					}
					catch (Exception ex)
					{
						failedCount++;
						logger.LogError(ex, "Failed to schedule pending job {JobId} of type {JobType}",
							job.Id, job.JobType);
					}
				}

				logger.LogInformation("Completed scheduling pending jobs on startup: {ScheduledCount} successful, {FailedCount} failed",
					scheduledCount, failedCount);
			}
			catch (OperationCanceledException)
			{
				logger.LogInformation("Job Startup Service start was canceled.");
				// Swallow cancellation to handle gracefully
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error occurred while checking for pending jobs on startup");
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Job Startup Service is stopping");
			return Task.CompletedTask;
		}
	}
}