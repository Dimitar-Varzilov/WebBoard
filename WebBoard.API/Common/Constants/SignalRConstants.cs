namespace WebBoard.API.Common.Constants
{
	/// <summary>
	/// Constants for SignalR method names and event types
	/// </summary>
	public static class SignalRConstants
	{
		/// <summary>
		/// SignalR method names for client-side handlers
		/// </summary>
		public static class Methods
		{
			/// <summary>
			/// Method name for job status updates
			/// </summary>
			public const string JobStatusUpdated = "JobStatusUpdated";

			/// <summary>
			/// Method name for job progress updates
			/// </summary>
			public const string JobProgressUpdated = "JobProgressUpdated";

			/// <summary>
			/// Method name for report generation notifications
			/// </summary>
			public const string ReportGenerated = "ReportGenerated";
		}

		/// <summary>
		/// SignalR group name patterns
		/// </summary>
		public static class Groups
		{
			/// <summary>
			/// Group name prefix for job-specific updates
			/// Format: job_{jobId}
			/// </summary>
			public const string JobPrefix = "job_";

			/// <summary>
			/// Gets the group name for a specific job
			/// </summary>
			/// <param name="jobId">The job ID</param>
			/// <returns>The group name</returns>
			public static string GetJobGroup(Guid jobId)
			{
				return $"{JobPrefix}{jobId}";
			}
		}
	}
}
