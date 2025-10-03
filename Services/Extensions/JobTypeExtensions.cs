using WebBoard.Services.Jobs;

namespace WebBoard.Services.Extensions
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
	}
}