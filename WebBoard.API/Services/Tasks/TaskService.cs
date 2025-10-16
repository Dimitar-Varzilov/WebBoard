using Microsoft.EntityFrameworkCore;
using Sieve.Services;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Services.Common;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Tasks
{
       public class TaskService(AppDbContext db, IQueryProcessor queryProcessor) : ITaskService
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

			// Prevent deletion of completed tasks
			if (task.Status == TaskItemStatus.Completed)
			{
				throw new InvalidOperationException("Cannot delete a completed task. Completed tasks are read-only.");
			}

			db.Tasks.Remove(task);
			await db.SaveChangesAsync();
			return true;
		}

		public async Task<PagedResult<TaskDto>> GetTasksAsync(TaskQueryParameters parameters)
		{
            var baseQuery = db.Tasks.AsNoTracking();

            // Custom filtering for Status and HasJob
            if (parameters.Status.HasValue)
            {
                baseQuery = baseQuery.Where(t => (int)t.Status == parameters.Status.Value);
            }
            if (parameters.HasJob.HasValue)
            {
                baseQuery = parameters.HasJob.Value
                    ? baseQuery.Where(t => t.JobId != null)
                    : baseQuery.Where(t => t.JobId == null);
            }

            // Apply case-insensitive search on Title and Description if SearchTerm is provided
            if (!string.IsNullOrWhiteSpace(parameters.Filters))
            {
                var searchTerm = parameters.Filters.ToLower();
                baseQuery = baseQuery.Where(t =>
                    t.Title.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    t.Description.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase));
            }

            // Use QueryProcessor for sorting and pagination
            return await queryProcessor.ApplyAsync(
                baseQuery,
                parameters,
                task => new TaskDto(
                    task.Id,
                    task.Title,
                    task.Description,
                    task.Status,
                    task.CreatedAt,
                    task.JobId)
            );
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

			// Prevent editing completed tasks
			if (task.Status == TaskItemStatus.Completed)
			{
				throw new InvalidOperationException("Cannot update a completed task. Completed tasks are read-only.");
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
