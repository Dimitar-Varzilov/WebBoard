using Microsoft.AspNetCore.SignalR;
using WebBoard.API.Common.Enums;

namespace WebBoard.API.Hubs
{
	/// <summary>
	/// SignalR Hub for real-time job status updates
	/// </summary>
	public class JobStatusHub(ILogger<JobStatusHub> logger) : Hub
	{

		/// <summary>
		/// Called when a client connects to the hub
		/// </summary>
		public override async Task OnConnectedAsync()
		{
			logger.LogInformation("Client {ConnectionId} connected to JobStatusHub", Context.ConnectionId);
			await base.OnConnectedAsync();
		}

		/// <summary>
		/// Called when a client disconnects from the hub
		/// </summary>
		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			logger.LogInformation("Client {ConnectionId} disconnected from JobStatusHub", Context.ConnectionId);
			await base.OnDisconnectedAsync(exception);
		}

		/// <summary>
		/// Allow clients to subscribe to specific job updates
		/// </summary>
		/// <param name="jobId">Job ID to subscribe to</param>
		public async Task SubscribeToJob(string jobId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"job_{jobId}");
			logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}", Context.ConnectionId, jobId);
		}

		/// <summary>
		/// Allow clients to unsubscribe from specific job updates
		/// </summary>
		/// <param name="jobId">Job ID to unsubscribe from</param>
		public async Task UnsubscribeFromJob(string jobId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_{jobId}");
			logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}", Context.ConnectionId, jobId);
		}
	}

	/// <summary>
	/// DTO for job status update notifications
	/// </summary>
	public record JobStatusUpdateDto(
		Guid JobId,
		string JobType,
		JobStatus Status,
		DateTimeOffset UpdatedAt,
		int? Progress = null,
		string? ErrorMessage = null,
		bool HasReport = false,
		Guid? ReportId = null,
		string? ReportFileName = null,
		int? TaskCount = null);
}
