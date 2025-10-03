using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Services.Jobs;

namespace WebBoard.Services.Extensions
{
	public static class QuartzHelper
	{
		public static void ConfigureQuartzJobs(IServiceCollectionQuartzConfigurator q)
		{
			// Register job classes
			q.AddJob<MarkTasksAsCompletedJob>(opts => opts
				.WithIdentity(Constants.JobTypes.MarkAllTasksAsDone)
				.StoreDurably());

			q.AddJob<GenerateTaskListJob>(opts => opts
				.WithIdentity(Constants.JobTypes.GenerateTaskReport)
				.StoreDurably());

			// Configure other job settings
			q.UseSimpleTypeLoader();
			q.UseInMemoryStore();
		}

		public static void RegisterScheduler(IServiceCollection services)
		{
			// Register IScheduler for dependency injection
			services.AddSingleton(provider =>
			{
				var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
				return schedulerFactory.GetScheduler().Result;
			});
		}
	}
}