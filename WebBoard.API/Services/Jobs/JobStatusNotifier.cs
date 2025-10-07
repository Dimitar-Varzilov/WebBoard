using Microsoft.AspNetCore.SignalR;
using WebBoard.API.Common.Enums;
using WebBoard.API.Hubs;

namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Implementation of job status notifications via SignalR
	/// </summary>
	public class JobStatusNotifier(
		IHubContext<JobStatusHub> hubContext,
		ILogger<JobStatusNotifier> logger) : IJobStatusNotifier
	{
		public async Task NotifyJobStatusAsync(Guid jobId, string jobType, JobStatus status, string? errorMessage = null)
		{
			try
			{
				var update = new JobStatusUpdateDto(
					JobId: jobId,
					JobType: jobType,
					Status: status,
					UpdatedAt: DateTimeOffset.UtcNow,
					ErrorMessage: errorMessage
				);

				// Broadcast to all clients
				await hubContext.Clients.All.SendAsync("JobStatusUpdated", update);

				// Also send to specific job group
				await hubContext.Clients.Group($"job_{jobId}")
					.SendAsync("JobStatusUpdated", update);

				logger.LogInformation(
					"Broadcasted job status update: Job {JobId} status changed to {Status}",
					jobId, status);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to broadcast job status update for job {JobId}", jobId);
			}
		}

		public async Task NotifyJobProgressAsync(Guid jobId, int progress)
		{
			try
			{
				await hubContext.Clients.All.SendAsync("JobProgressUpdated", new
				{
					JobId = jobId,
					Progress = progress,
					UpdatedAt = DateTimeOffset.UtcNow
				});

				logger.LogDebug("Broadcasted job progress: Job {JobId} progress {Progress}%", jobId, progress);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to broadcast job progress for job {JobId}", jobId);
			}
		}

		public async Task NotifyReportGeneratedAsync(Guid jobId, Guid reportId, string fileName)
		{
			try
			{
				var update = new JobStatusUpdateDto(
					JobId: jobId,
					JobType: string.Empty,
					Status: JobStatus.Completed,
					UpdatedAt: DateTimeOffset.UtcNow,
					HasReport: true,
					ReportId: reportId,
					ReportFileName: fileName
				);

				await hubContext.Clients.All.SendAsync("ReportGenerated", update);

				logger.LogInformation(
					"Broadcasted report generation: Job {JobId} generated report {ReportId}",
					jobId, reportId);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to broadcast report generation for job {JobId}", jobId);
			}
		}

		public async Task NotifyJobGroupAsync(Guid jobId, string jobType, JobStatus status)
		{
			try
			{
				var update = new JobStatusUpdateDto(
					JobId: jobId,
					JobType: jobType,
					Status: status,
					UpdatedAt: DateTimeOffset.UtcNow
				);

				await hubContext.Clients.Group($"job_{jobId}")
					.SendAsync("JobStatusUpdated", update);

				logger.LogDebug("Notified job group for job {JobId}", jobId);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to notify job group for job {JobId}", jobId);
			}
		}
	}
}
