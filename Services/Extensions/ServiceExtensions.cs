using Microsoft.EntityFrameworkCore;
using Quartz;
using WebBoard.Data;

namespace WebBoard.Services.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Add DbContext
			services.AddDbContext<AppDbContext>(options =>
				options.UseInMemoryDatabase("WebBoardDb")); // For demo purposes, use InMemoryDatabase

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