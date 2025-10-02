namespace WebBoard.Services
{
	public interface IBackgroundService
	{
		Task ExecuteAsync(CancellationToken stoppingToken);

	}
}
