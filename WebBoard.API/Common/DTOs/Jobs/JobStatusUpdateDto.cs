using WebBoard.API.Common.Enums;

namespace WebBoard.API.Common.DTOs.Jobs
{
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
