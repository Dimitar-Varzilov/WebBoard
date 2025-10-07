using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Service for managing job retry logic with database tracking
	/// </summary>
	public class JobRetryService(
		AppDbContext dbContext,
		IJobSchedulingService jobSchedulingService,
		ILogger<JobRetryService> logger) : IJobRetryService
	{
		private const int DefaultMaxRetries = 3;

		public async Task<bool> ShouldRetryJobAsync(Guid jobId)
		{
			var retryInfo = await dbContext.JobRetries
				.Where(r => r.JobId == jobId)
				.FirstOrDefaultAsync();

			return retryInfo == null || retryInfo.RetryCount < retryInfo.MaxRetries;
		}

		public async Task ScheduleRetryAsync(Guid jobId, string errorMessage)
		{
			var retryInfo = await dbContext.JobRetries
				.Where(r => r.JobId == jobId)
				.FirstOrDefaultAsync();

			if (retryInfo == null)
			{
				// First retry
				retryInfo = new JobRetryInfo(
					Id: Guid.NewGuid(),
					JobId: jobId,
					RetryCount: 0,
					MaxRetries: DefaultMaxRetries,
					NextRetryAt: DateTimeOffset.UtcNow.AddMinutes(1),
					LastErrorMessage: errorMessage,
					CreatedAt: DateTimeOffset.UtcNow);

				dbContext.JobRetries.Add(retryInfo);
				logger.LogInformation("Created retry tracking for job {JobId}", jobId);
			}
			else
			{
				// Subsequent retry
				var nextRetryCount = retryInfo.RetryCount + 1;
				var nextRetryDelay = CalculateRetryDelay(nextRetryCount);

				var updatedRetryInfo = retryInfo with
				{
					RetryCount = nextRetryCount,
					NextRetryAt = DateTimeOffset.UtcNow.Add(nextRetryDelay),
					LastErrorMessage = errorMessage
				};

				dbContext.Entry(retryInfo).CurrentValues.SetValues(updatedRetryInfo);
				logger.LogInformation("Updated retry tracking for job {JobId}, attempt {Attempt}/{Max}",
					jobId, nextRetryCount + 1, retryInfo.MaxRetries);
			}

			await dbContext.SaveChangesAsync();

			// Schedule the retry job
			var job = await dbContext.Jobs.FindAsync(jobId);
			if (job != null)
			{
				var retryJob = job with { ScheduledAt = retryInfo.NextRetryAt };
				await jobSchedulingService.ScheduleJobAsync(retryJob);

				logger.LogInformation(
					"Scheduled retry for job {JobId} at {RetryTime} (Attempt {Attempt}/{Max})",
					jobId, retryInfo.NextRetryAt, retryInfo.RetryCount + 1, retryInfo.MaxRetries);
			}
			else
			{
				logger.LogError("Job {JobId} not found, cannot schedule retry", jobId);
			}
		}

		public async Task<JobRetryInfo?> GetRetryInfoAsync(Guid jobId)
		{
			return await dbContext.JobRetries
				.Where(r => r.JobId == jobId)
				.FirstOrDefaultAsync();
		}

		public async Task RemoveRetryInfoAsync(Guid jobId)
		{
			var retryInfo = await dbContext.JobRetries
				.Where(r => r.JobId == jobId)
				.FirstOrDefaultAsync();

			if (retryInfo != null)
			{
				dbContext.JobRetries.Remove(retryInfo);
				await dbContext.SaveChangesAsync();
				logger.LogInformation("Removed retry tracking for job {JobId} (job succeeded)", jobId);
			}
		}

		/// <summary>
		/// Calculates retry delay using exponential backoff with jitter
		/// </summary>
		private static TimeSpan CalculateRetryDelay(int retryCount)
		{
			// Exponential backoff: 1min, 2min, 4min
			var baseDelayMinutes = Math.Pow(2, retryCount - 1);
			
			// Add jitter (0-30 seconds) to prevent thundering herd
			var jitterSeconds = Random.Shared.Next(0, 30);
			
			return TimeSpan.FromMinutes(baseDelayMinutes) + TimeSpan.FromSeconds(jitterSeconds);
		}
	}
}
