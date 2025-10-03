using WebBoard.Common.Enums;

namespace WebBoard.Common.Models
{
    public record Job(Guid Id, string JobType, JobStatus Status, DateTime CreatedAt, DateTime? ScheduledAt = null)
    {
        public ICollection<TaskItem> Tasks { get; init; } = [];
    }
}