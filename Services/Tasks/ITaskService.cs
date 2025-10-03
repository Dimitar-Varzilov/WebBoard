using WebBoard.Common.DTOs.Tasks;

namespace WebBoard.Services.Tasks
{
	public interface ITaskService
	{
		Task<IEnumerable<TaskDto>> GetAllTasksAsync();
		Task<TaskDto?> GetTaskByIdAsync(Guid id);
		Task<TaskDto> CreateTaskAsync(CreateTaskRequestDto createTaskRequest);
		Task<TaskDto?> UpdateTaskAsync(Guid id, UpdateTaskRequestDto updateTaskRequest);
		Task<bool> DeleteTaskAsync(Guid id);
	}
}
