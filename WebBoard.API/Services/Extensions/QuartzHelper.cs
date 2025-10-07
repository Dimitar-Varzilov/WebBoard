using Quartz;

namespace WebBoard.API.Services.Extensions
{
	public static class QuartzHelper
	{
		public static void ConfigureQuartzJobs(IServiceCollectionQuartzConfigurator q)
		{
			// Configure Quartz settings for database-driven job scheduling
			q.UseSimpleTypeLoader();
			q.UseInMemoryStore();
			q.UseDefaultThreadPool(tp =>
			{
				tp.MaxConcurrency = 10; // Allow multiple jobs to run concurrently
			});
		}
	}
}