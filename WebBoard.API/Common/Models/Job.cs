using WebBoard.API.Common.Enums;

namespace WebBoard.API.Common.Models
{
	public record Job(Guid Id, string JobType, JobStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? ScheduledAt = null)
	{
		public ICollection<TaskItem> Tasks { get; init; } = [];
		public Report? Report { get; init; }
	}
}