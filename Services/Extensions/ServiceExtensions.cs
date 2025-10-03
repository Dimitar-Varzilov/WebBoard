using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Data;
using WebBoard.Services.Jobs;
using WebBoard.Services.Tasks;

namespace WebBoard.Services.Extensions
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

			services.AddSingleton<IBackgroundService, BackgroundService>();

			// Configure Quartz
			services.AddQuartz(QuartzHelper.ConfigureQuartzJobs);

			// Add Quartz hosted service
			services.AddQuartzHostedService(options =>
			{
				options.WaitForJobsToComplete = true;
			});

			// Register IScheduler for dependency injection
			QuartzHelper.RegisterScheduler(services);


			return services;
		}
	}
}