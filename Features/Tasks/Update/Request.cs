using Microsoft.AspNetCore.Mvc;
using WebBoard.Common.Enums;

namespace WebBoard.Features.Tasks.Update
{
    public class UpdateTaskRequest
    {
        [FromRoute]
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required TaskItemStatus Status { get; set; }
    }
}
