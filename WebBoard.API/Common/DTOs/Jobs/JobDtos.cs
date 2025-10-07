using System.ComponentModel.DataAnnotations;
using WebBoard.API.Common.Attributes;
using WebBoard.API.Common.Enums;

namespace WebBoard.API.Common.DTOs.Jobs
{
	public record JobDto(
		Guid Id,
		string JobType,
		JobStatus Status,
		DateTimeOffset CreatedAt,
		DateTimeOffset? ScheduledAt,
		bool HasReport = false,
		Guid? ReportId = null,
		string? ReportFileName = null,
		IEnumerable<Guid>? TaskIds = null);

	public record CreateJobRequestDto(
		[Required(ErrorMessage = "Job type is required")]
		string JobType,
		bool RunImmediately = true,
		[NotInPastOffset(ErrorMessage = "Scheduled time cannot be in the past")]
		DateTimeOffset? ScheduledAt = null,
		[Required(ErrorMessage = "Task selection is required")]
		[MinLength(1, ErrorMessage = "At least one task must be selected")]
		IEnumerable<Guid> TaskIds = null!);
}
