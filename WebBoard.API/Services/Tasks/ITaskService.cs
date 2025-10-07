using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;

namespace WebBoard.API.Services.Tasks
{
	public interface ITaskService
	{
		Task<PagedResult<TaskDto>> GetTasksAsync(TaskQueryParameters parameters);
		Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(TaskItemStatus status);
		Task<int> GetTaskCountByStatusAsync(TaskItemStatus status);
		Task<TaskDto?> GetTaskByIdAsync(Guid id);
		Task<TaskDto> CreateTaskAsync(CreateTaskRequestDto createTaskRequest);
		Task<TaskDto?> UpdateTaskAsync(Guid id, UpdateTaskRequestDto updateTaskRequest);
		Task<bool> DeleteTaskAsync(Guid id);
	}
}
