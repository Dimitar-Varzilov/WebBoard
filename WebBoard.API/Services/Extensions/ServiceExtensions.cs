using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Reports;
using WebBoard.API.Services.Tasks;

namespace WebBoard.API.Services.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Add DbContext with PostgreSQL
			services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

			services.AddScoped<ITaskService, TaskService>();
			services.AddScoped<IJobService, JobService>();
			services.AddScoped<IJobSchedulingService, JobSchedulingService>();
			services.AddScoped<IJobCleanupService, JobCleanupService>();
			services.AddScoped<IReportService, ReportService>();

			// Register SignalR job status notifier
			services.AddScoped<IJobStatusNotifier, JobStatusNotifier>();

			// Register job type registry as singleton since it's stateless
			services.AddSingleton<IJobTypeRegistry, JobTypeRegistry>();

			// Configure job cleanup options
			services.Configure<JobCleanupOptions>(configuration.GetSection("JobCleanup"));

			// Configure Quartz for database-driven job scheduling
			services.AddQuartz(QuartzHelper.ConfigureQuartzJobs);

			// Add Quartz hosted service
			services.AddQuartzHostedService(options =>
			{
				options.WaitForJobsToComplete = true;
			});

			// Register IScheduler for dependency injection
			services.AddSingleton(provider =>
			{
				var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
				return schedulerFactory.GetScheduler().Result;
			});

			// Register startup service to check for pending jobs on application start
			services.AddHostedService<JobStartupService>();

			return services;
		}
	}
}