namespace WebBoard.Services
{
	public interface IBackgroundService
	{
		public Task ExecuteAsync(CancellationToken stoppingToken);

	}
}
