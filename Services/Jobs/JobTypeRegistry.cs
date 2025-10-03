using System.Reflection;
using Quartz;
using WebBoard.Common.Constants;

namespace WebBoard.Services.Jobs
{
	public interface IJobTypeRegistry
	{
		Type GetJobType(string jobTypeName);
		bool IsValidJobType(string jobTypeName);
		IEnumerable<string> GetAllJobTypes();
	}

	public class JobTypeRegistry : IJobTypeRegistry
	{
		private readonly Dictionary<string, Type> _jobTypeMap;

		public JobTypeRegistry()
		{
			_jobTypeMap = DiscoverJobTypes();
		}

		public Type GetJobType(string jobTypeName)
		{
			if (string.IsNullOrWhiteSpace(jobTypeName))
				throw new ArgumentException("Job type name cannot be null or empty", nameof(jobTypeName));

			if (!_jobTypeMap.TryGetValue(jobTypeName, out var jobType))
				throw new InvalidOperationException($"Unknown job type: '{jobTypeName}'. Available types: {string.Join(", ", GetAllJobTypes())}");

			return jobType;
		}

		public bool IsValidJobType(string jobTypeName)
		{
			return !string.IsNullOrWhiteSpace(jobTypeName) && _jobTypeMap.ContainsKey(jobTypeName);
		}

		public IEnumerable<string> GetAllJobTypes()
		{
			return _jobTypeMap.Keys;
		}

		/// <summary>
		/// Auto-discovers job types in the current assembly using the JobType attribute
		/// </summary>
		private static Dictionary<string, Type> DiscoverJobTypes()
		{
			var jobTypeMap = new Dictionary<string, Type>();

			// Get all types in the current assembly that implement IJob
			var assembly = Assembly.GetExecutingAssembly();
			var jobTypes = assembly.GetTypes()
				.Where(type => typeof(IJob).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);

			foreach (var jobType in jobTypes)
			{
				var jobTypeAttribute = jobType.GetCustomAttribute<JobTypeAttribute>();
				if (jobTypeAttribute != null)
				{
					jobTypeMap[jobTypeAttribute.JobTypeName] = jobType;
				}
			}

			// Fallback: Add manual mappings for any jobs without attributes (backward compatibility)
			if (!jobTypeMap.ContainsKey(Constants.JobTypes.MarkAllTasksAsDone))
			{
				jobTypeMap[Constants.JobTypes.MarkAllTasksAsDone] = typeof(MarkTasksAsCompletedJob);
			}

			if (!jobTypeMap.ContainsKey(Constants.JobTypes.GenerateTaskReport))
			{
				jobTypeMap[Constants.JobTypes.GenerateTaskReport] = typeof(GenerateTaskListJob);
			}

			return jobTypeMap;
		}

		/// <summary>
		/// Manually registers a new job type. This method can be used to dynamically add job types.
		/// </summary>
		/// <param name="jobTypeName">The string identifier for the job type</param>
		/// <param name="jobType">The Type that implements IJob</param>
		public void RegisterJobType(string jobTypeName, Type jobType)
		{
			if (string.IsNullOrWhiteSpace(jobTypeName))
				throw new ArgumentException("Job type name cannot be null or empty", nameof(jobTypeName));

			ArgumentNullException.ThrowIfNull(jobType);

			if (!typeof(IJob).IsAssignableFrom(jobType))
				throw new ArgumentException($"Job type {jobType.Name} must implement IJob interface", nameof(jobType));

			_jobTypeMap[jobTypeName] = jobType;
		}
	}
}