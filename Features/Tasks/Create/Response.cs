using WebBoard.Common.Enums;

namespace WebBoard.Features.Tasks.Create
{
	public record TaskResponse(Guid Id, string Title, string Description, TaskItemStatus TaskItemStatus, DateTime CreatedAt);
}
