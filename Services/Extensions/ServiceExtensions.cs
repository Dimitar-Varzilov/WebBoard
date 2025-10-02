using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Data;

namespace WebBoard.Services.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Add DbContext with PostgreSQL
			services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

			// Configure Quartz
			services.AddQuartz(QuartzHelper.ConfigureQuartzJobs);

			// Add Quartz hosted service
			services.AddQuartzHostedService(options =>
			{
				options.WaitForJobsToComplete = true;
			});

			// Register IScheduler for dependency injection
			QuartzHelper.RegisterScheduler(services);

			// Register background services
			services.AddSingleton<IBackgroundService, BackgroundService>();

			return services;
		}
	}
}