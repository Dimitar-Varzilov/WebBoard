using Quartz;
using WebBoard.Services.Jobs;

namespace WebBoard.Services.Extensions
{
	public static class QuartzHelper
	{
		public static void ConfigureQuartzJobs(IServiceCollectionQuartzConfigurator q)
		{
			// Register job classes
			q.AddJob<MarkTasksAsCompletedJob>(opts => opts
				.WithIdentity("MarkTasksAsCompleted")
				.StoreDurably());

			q.AddJob<GenerateTaskListJob>(opts => opts
				.WithIdentity("GenerateTaskList")
				.StoreDurably());

			// Create triggers for MarkTasksAsCompleted
			q.AddTrigger(opts => opts
				.ForJob("MarkTasksAsCompleted")
				.WithIdentity("MarkTasksAsCompleted-8AM")
				.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(8, 0)));

			q.AddTrigger(opts => opts
				.ForJob("MarkTasksAsCompleted")
				.WithIdentity("MarkTasksAsCompleted-2PM")
				.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(14, 0)));

			// Create triggers for GenerateTaskList
			q.AddTrigger(opts => opts
				.ForJob("GenerateTaskList")
				.WithIdentity("GenerateTaskList-8AM")
				.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(8, 0)));

			q.AddTrigger(opts => opts
				.ForJob("GenerateTaskList")
				.WithIdentity("GenerateTaskList-2PM")
				.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(14, 0)));

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