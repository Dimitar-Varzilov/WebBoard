using System.ComponentModel.DataAnnotations;
using WebBoard.Common.Enums;

namespace WebBoard.Common.DTOs.Jobs
{
	public record JobDto(Guid Id, string JobType, JobStatus Status, DateTime CreatedAt);

	public record CreateJobRequestDto([Required] string JobType);
}
