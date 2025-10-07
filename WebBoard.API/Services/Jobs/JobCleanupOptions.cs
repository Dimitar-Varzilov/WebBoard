namespace WebBoard.Services.Jobs
{
	public class JobCleanupOptions
	{
		/// <summary>
		/// Whether to automatically clean up completed jobs from scheduler and/or database
		/// </summary>
		public bool AutoCleanupCompletedJobs { get; set; } = true;

		/// <summary>
		/// Whether to clean up the job from the database
		/// WARNING: Setting this to true will remove audit trail of job executions
		/// RECOMMENDED: Keep this false to maintain complete job history for debugging and compliance
		/// </summary>
		public bool RemoveFromDatabase { get; set; } = false;

		/// <summary>
		/// Whether to clean up the job from the Quartz scheduler
		/// RECOMMENDED: Keep this true to prevent scheduler bloat while preserving database records
		/// </summary>
		public bool RemoveFromScheduler { get; set; } = true;

		/// <summary>
		/// How long to retain completed jobs before cleanup (only applies if auto cleanup is enabled)
		/// Set to TimeSpan.Zero for immediate cleanup
		/// This applies to both scheduler and database cleanup (if enabled)
		/// </summary>
		public TimeSpan RetentionPeriod { get; set; } = TimeSpan.Zero;
	}
}