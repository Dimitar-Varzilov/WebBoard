using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
    public abstract class BaseJob(IServiceProvider serviceProvider, ILogger logger)
    {
        protected async Task ExecuteWithDelayAsync(Func<AppDbContext, CancellationToken, Task> action, string phase, CancellationToken ct)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            logger.LogInformation("Starting {Phase} phase", phase);
            await Task.Delay(TimeSpan.FromMinutes(3), ct);
            await action(dbContext, ct);
            logger.LogInformation("Completed {Phase} phase", phase);
        }
    }
}