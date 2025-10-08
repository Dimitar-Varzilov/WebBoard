using System.ComponentModel.DataAnnotations;

namespace WebBoard.API.Common.DTOs.Jobs
{
	/// <summary>
	/// DTO for updating an existing job (only queued jobs can be updated)
	/// </summary>
	public record UpdateJobRequestDto(
		[Required] string JobType,
		bool RunImmediately,
		DateTimeOffset? ScheduledAt,
		[Required] IEnumerable<Guid> TaskIds);
}
