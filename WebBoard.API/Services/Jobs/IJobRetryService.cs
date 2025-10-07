using WebBoard.API.Common.Models;

namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Service for managing job retry logic
	/// </summary>
	public interface IJobRetryService
	{
		/// <summary>
		/// Checks if a job should be retried
		/// </summary>
		Task<bool> ShouldRetryJobAsync(Guid jobId);

		/// <summary>
		/// Schedules a retry for a failed job
		/// </summary>
		Task ScheduleRetryAsync(Guid jobId, string errorMessage);

		/// <summary>
		/// Gets retry information for a job
		/// </summary>
		Task<JobRetryInfo?> GetRetryInfoAsync(Guid jobId);

		/// <summary>
		/// Removes retry tracking for a job (called on success)
		/// </summary>
		Task RemoveRetryInfoAsync(Guid jobId);
	}
}
