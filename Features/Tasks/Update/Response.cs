using WebBoard.Common.Enums;

namespace WebBoard.Features.Tasks.Update
{
    public record UpdateTaskResponse(Guid Id, string Title, string Description, TaskItemStatus Status, DateTime CreatedAt);
}
