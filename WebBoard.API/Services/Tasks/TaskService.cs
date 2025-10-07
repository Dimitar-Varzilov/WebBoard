using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Extensions;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Tasks
{
	public class TaskService(AppDbContext db) : ITaskService
	{
		public async Task<TaskDto> CreateTaskAsync(CreateTaskRequestDto createTaskRequest)
		{
			var task = new TaskItem(
				Guid.NewGuid(),
				DateTimeOffset.UtcNow,
				createTaskRequest.Title,
				createTaskRequest.Description,
				TaskItemStatus.Pending,
				null
			);

			db.Tasks.Add(task);
			await db.SaveChangesAsync();

			return new TaskDto(task.Id, task.Title, task.Description, task.Status, task.CreatedAt, task.JobId);
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

		public async Task<PagedResult<TaskDto>> GetTasksAsync(TaskQueryParameters parameters)
		{
			var query = db.Tasks.AsNoTracking();

			// Apply filtering
			if (parameters.Status.HasValue)
			{
				query = query.Where(t => (int)t.Status == parameters.Status.Value);
			}

			if (parameters.HasJob.HasValue)
			{
				query = parameters.HasJob.Value
					? query.Where(t => t.JobId != null)
					: query.Where(t => t.JobId == null);
			}

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.ToLower();
				query = query.Where(t =>
					t.Title.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
					t.Description.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase));
			}

			// Apply sorting
			query = query.ApplySort(parameters.SortBy ?? "CreatedAt", parameters.IsAscending);

			// Project to DTO
			var dtoQuery = query.Select(task => new TaskDto(
				task.Id,
				task.Title,
				task.Description,
				task.Status,
				task.CreatedAt,
				task.JobId));

			// Apply pagination and return result
			return await dtoQuery.ToPagedResultAsync(parameters);
		}
		public async Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(TaskItemStatus status)
		{
			return await db.Tasks
				.AsNoTracking()
				.Where(task => task.Status == status)
				.OrderBy(task => task.CreatedAt)
				.Select(task => new TaskDto(task.Id, task.Title, task.Description, task.Status, task.CreatedAt, task.JobId))
				.ToListAsync();
		}

		/// <summary>
		/// Gets the count of tasks by status without materializing objects - highly efficient
		/// Uses EF Core CountAsync() which generates SELECT COUNT(*) query
		/// </summary>
		/// <param name="status">The task status to count</param>
		/// <returns>Count of tasks with the specified status</returns>
		public async Task<int> GetTaskCountByStatusAsync(TaskItemStatus status)
		{
			return await db.Tasks
				.AsNoTracking()
				.Where(task => task.Status == status)
				.CountAsync();
		}

		public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
		{
			var task = await db.Tasks.AsNoTracking()
								  .FirstOrDefaultAsync(t => t.Id == id);

			return task == null ? null : new TaskDto(task.Id, task.Title, task.Description, task.Status, task.CreatedAt, task.JobId);
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

			return new TaskDto(updatedTask.Id, updatedTask.Title, updatedTask.Description, updatedTask.Status, updatedTask.CreatedAt, updatedTask.JobId);
		}
	}
}
