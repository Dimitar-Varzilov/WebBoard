using WebBoard.API.Common.Enums;

namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Service for broadcasting job status updates via SignalR
	/// </summary>
	public interface IJobStatusNotifier
	{
		/// <summary>
		/// Notify all clients about job status update
		/// </summary>
		Task NotifyJobStatusAsync(Guid jobId, string jobType, JobStatus status, string? errorMessage = null);

		/// <summary>
		/// Notify all clients about job progress update
		/// </summary>
		Task NotifyJobProgressAsync(Guid jobId, int progress);

		/// <summary>
		/// Notify all clients about report generation
		/// </summary>
		Task NotifyReportGeneratedAsync(Guid jobId, Guid reportId, string fileName);

		/// <summary>
		/// Notify specific job group about status update
		/// </summary>
		Task NotifyJobGroupAsync(Guid jobId, string jobType, JobStatus status);
	}
}
