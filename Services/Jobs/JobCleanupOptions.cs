namespace WebBoard.Services.Jobs
{
	public class JobCleanupOptions
	{
		/// <summary>
		/// Whether to automatically clean up completed jobs from scheduler and database
		/// </summary>
		public bool AutoCleanupCompletedJobs { get; set; } = true;

		/// <summary>
		/// Whether to clean up the job from the database (if false, only removes from scheduler)
		/// </summary>
		public bool RemoveFromDatabase { get; set; } = true;

		/// <summary>
		/// Whether to clean up the job from the Quartz scheduler
		/// </summary>
		public bool RemoveFromScheduler { get; set; } = true;

		/// <summary>
		/// How long to retain completed jobs before cleanup (only applies if auto cleanup is enabled)
		/// Set to TimeSpan.Zero for immediate cleanup
		/// </summary>
		public TimeSpan RetentionPeriod { get; set; } = TimeSpan.Zero;
	}
}