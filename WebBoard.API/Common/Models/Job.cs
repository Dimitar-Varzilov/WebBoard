using WebBoard.API.Common.Enums;
using Sieve.Attributes;

namespace WebBoard.API.Common.Models
{
    public record Job
    {
        public Guid Id { get; init; }

        [Sieve(CanSort = true, CanFilter = true)]
        public string JobType { get; init; } = string.Empty;

        [Sieve(CanSort = true, CanFilter = true)]
        public JobStatus Status { get; init; }

        [Sieve(CanSort = true)]
        public DateTimeOffset CreatedAt { get; init; }

        [Sieve(CanSort = true, CanFilter = true)]
        public DateTimeOffset? ScheduledAt { get; init; }

        public ICollection<TaskItem> Tasks { get; init; } = [];
        public Report? Report { get; init; }

        public Job(Guid id, string jobType, JobStatus status, DateTimeOffset createdAt, DateTimeOffset? scheduledAt = null)
        {
            Id = id;
            JobType = jobType;
            Status = status;
            CreatedAt = createdAt;
            ScheduledAt = scheduledAt;
        }
    }
}
