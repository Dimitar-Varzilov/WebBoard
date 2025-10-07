using WebBoard.API.Services.Jobs;

namespace WebBoard.API.Services.Extensions
{
	public static class JobTypeExtensions
	{
		/// <summary>
		/// Extension method to validate job types in controllers or services
		/// </summary>
		public static bool IsValidJobType(this string jobTypeName, IJobTypeRegistry jobTypeRegistry)
		{
			return jobTypeRegistry.IsValidJobType(jobTypeName);
		}

		/// <summary>
		/// Extension method to get all available job types for API responses
		/// </summary>
		public static IEnumerable<string> GetAvailableJobTypes(this IJobTypeRegistry jobTypeRegistry)
		{
			return jobTypeRegistry.GetAllJobTypes();
		}

		/// <summary>
		/// Extension method to clean up a completed job (preserves database record by default)
		/// </summary>
		public static async Task CleanupIfCompleted(this Guid jobId, IJobCleanupService jobCleanupService)
		{
			await jobCleanupService.CleanupCompletedJobAsync(jobId);
		}

		/// <summary>
		/// Extension method to safely clean up job from scheduler only (preserves database record)
		/// </summary>
		public static async Task CleanupFromSchedulerOnly(this Guid jobId, IJobCleanupService jobCleanupService)
		{
			await jobCleanupService.CleanupFromSchedulerOnlyAsync(jobId);
		}
	}
}