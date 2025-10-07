namespace WebBoard.API.Common.Constants
{
	public static class Constants
	{
		public static class JobTypes
		{
			public const string MarkAllTasksAsDone = "MarkAllTasksAsDone";
			public const string GenerateTaskReport = "GenerateTaskReport";

		}

		public static class SwaggerTags
		{
			public const string Tasks = "Tasks";
			public const string Jobs = "Jobs";
			public const string Reports = "Reports";
		}

		public static class JobDataKeys
		{
			public const string JobId = "JobId";
		}

		public static class Timing
		{
			public const int RefreshSpinnerDuration = 1000;
			public const int AutoRefreshInterval = 5000;
			public const int JobMonitoringInterval = 2000;
		}
	}
}
