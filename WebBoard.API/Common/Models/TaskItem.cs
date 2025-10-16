using Sieve.Attributes;
using WebBoard.API.Common.Enums;

namespace WebBoard.API.Common.Models
{
    public record TaskItem(
        Guid Id,
        DateTimeOffset CreatedAt,
        string Title,
        string Description,
        TaskItemStatus Status,
        Guid? JobId)
    {
        public Guid Id { get; init; } = Id;
        [Sieve(CanSort = true)]
        public DateTimeOffset CreatedAt { get; init; } = CreatedAt;
        [Sieve(CanSort = true, CanFilter = true)]
        public string Title { get; init; } = Title;
        [Sieve(CanSort = true, CanFilter = true)]
        public string Description { get; init; } = Description;
        [Sieve(CanSort = true, CanFilter = true)]
        public TaskItemStatus Status { get; init; } = Status;
        public Guid? JobId { get; init; } = JobId;
        public Job? Job { get; init; }
    }
}