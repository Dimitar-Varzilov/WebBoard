using WebBoard.Common.Enums;

namespace WebBoard.Common.Models
{
	public record TaskItem(
		Guid Id,
		DateTimeOffset CreatedAt,
		string Title,
		string Description,
		TaskItemStatus Status,
		Guid? JobId)
	{
		public Job? Job { get; init; }
	}
}