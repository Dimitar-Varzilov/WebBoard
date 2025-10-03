using System.ComponentModel.DataAnnotations;
using WebBoard.Common.Enums;

namespace WebBoard.Common.DTOs.Jobs
{
	public record JobDto(Guid Id, string JobType, JobStatus Status, DateTime CreatedAt, DateTime? ScheduledAt);

	public record CreateJobRequestDto([Required] string JobType, bool RunImmediately = true, DateTime? ScheduledAt = null);
}
