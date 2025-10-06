using System.ComponentModel.DataAnnotations;
using WebBoard.Common.Attributes;
using WebBoard.Common.Enums;

namespace WebBoard.Common.DTOs.Jobs
{
	public record JobDto(
		Guid Id, 
		string JobType, 
		JobStatus Status, 
		DateTimeOffset CreatedAt, 
		DateTimeOffset? ScheduledAt, 
		bool HasReport = false, 
		Guid? ReportId = null,
		string? ReportFileName = null);

	public record CreateJobRequestDto(
		[Required(ErrorMessage = "Job type is required")]
		string JobType, 
		bool RunImmediately = true, 
		[NotInPastOffset(ErrorMessage = "Scheduled time cannot be in the past")]
		DateTimeOffset? ScheduledAt = null);
}
