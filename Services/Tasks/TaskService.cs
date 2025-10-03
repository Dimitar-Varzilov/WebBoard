using Microsoft.EntityFrameworkCore;
using WebBoard.Common.DTOs.Tasks;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Tasks
{
	public class TaskService(AppDbContext db) : ITaskService
	{
		public async Task<TaskDto> CreateTaskAsync(CreateTaskRequestDto createTaskRequest)
		{
			var task = new TaskItem(
				Guid.NewGuid(),
				DateTime.UtcNow,
				createTaskRequest.Title,
				createTaskRequest.Description,
				Common.Enums.TaskItemStatus.Pending,
				null
			);

			db.Tasks.Add(task);
			await db.SaveChangesAsync();

			return new TaskDto(task.Id, task.Title, task.Description, task.Status, task.CreatedAt);
		}

		public async Task<bool> DeleteTaskAsync(Guid id)
		{
			var task = await db.Tasks.FindAsync(id);
			if (task == null)
			{
				return false;
			}

			db.Tasks.Remove(task);
			await db.SaveChangesAsync();
			return true;
		}

		public async Task<IEnumerable<TaskDto>> GetAllTasksAsync()
		{
			return await db.Tasks
				.AsNoTracking()
				.Select(task => new TaskDto(task.Id, task.Title, task.Description, task.Status, task.CreatedAt))
				.ToListAsync();
		}

		public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
		{
			var task = await db.Tasks.AsNoTracking()
								  .FirstOrDefaultAsync(t => t.Id == id);

			return task == null ? null : new TaskDto(task.Id, task.Title, task.Description, task.Status, task.CreatedAt);
		}

		public async Task<TaskDto?> UpdateTaskAsync(Guid id, UpdateTaskRequestDto updateTaskRequest)
		{
			var task = await db.Tasks.FindAsync(id);

			if (task == null)
			{
				return null;
			}

			var updatedTask = task with
			{
				Title = updateTaskRequest.Title,
				Description = updateTaskRequest.Description,
				Status = updateTaskRequest.Status
			};

			db.Entry(task).CurrentValues.SetValues(updatedTask);
			await db.SaveChangesAsync();

			return new TaskDto(updatedTask.Id, updatedTask.Title, updatedTask.Description, updatedTask.Status, updatedTask.CreatedAt);
		}
	}
}
