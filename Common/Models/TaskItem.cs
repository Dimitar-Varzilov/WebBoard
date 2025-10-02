using WebBoard.Common.Enums;

namespace WebBoard.Common.Models
{
	public record TaskItem(Guid Id, DateTime CreatedAt, string Title, string Description, TaskItemStatus Status);
}