using Microsoft.AspNetCore.Mvc;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Services.Tasks;

namespace WebBoard.API.Controllers
{
	[ApiController]
	[Route("api/tasks")]
	[Tags(Constants.SwaggerTags.Tasks)]
	public class TasksController(ITaskService taskService) : ControllerBase
	{
		/// <summary>
		/// Get tasks with pagination, filtering, and sorting
		/// </summary>
		/// <param name="parameters">Query parameters for pagination, filtering, and sorting</param>
		/// <returns>A paginated list of tasks with metadata</returns>
		[HttpGet]
		[ProducesResponseType(typeof(PagedResult<TaskDto>), 200)]
		public async Task<IActionResult> GetTasks([FromQuery] TaskQueryParameters parameters)
		{
			var result = await taskService.GetTasksAsync(parameters);
			return Ok(result);
		}

		/// <summary>
		/// Get tasks by specific status
		/// </summary>
		/// <param name="status">The task status to filter by (Pending, InProgress, Completed)</param>
		/// <returns>A list of tasks with the specified status</returns>
		[HttpGet("status/{status}")]
		[ProducesResponseType(typeof(IEnumerable<TaskDto>), 200)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> GetTasksByStatus(TaskItemStatus status)
		{
			if (!Enum.IsDefined(typeof(TaskItemStatus), status))
			{
				return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames<TaskItemStatus>())}");
			}

			var tasks = await taskService.GetTasksByStatusAsync(status);
			return Ok(tasks);
		}

		/// <summary>
		/// Get count of tasks by specific status (lightweight - no object materialization)
		/// </summary>
		/// <param name="status">The task status to count (Pending, InProgress, Completed)</param>
		/// <returns>Count of tasks with the specified status</returns>
		[HttpGet("status/{status}/count")]
		[ProducesResponseType(typeof(int), 200)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> GetTaskCountByStatus(TaskItemStatus status)
		{
			if (!Enum.IsDefined(typeof(TaskItemStatus), status))
			{
				return BadRequest($"Invalid status. Valid values are: {string.Join(", ", Enum.GetNames<TaskItemStatus>())}");
			}

			var count = await taskService.GetTaskCountByStatusAsync(status);
			return Ok(count);
		}

		/// <summary>
		/// Get a specific task by ID
		/// </summary>
		/// <param name="id">The unique identifier of the task</param>
		/// <returns>The task with the specified ID</returns>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(TaskDto), 200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> GetTaskById(Guid id)
		{
			var task = await taskService.GetTaskByIdAsync(id);
			return task == null ? NotFound() : Ok(task);
		}

		/// <summary>
		/// Create a new task
		/// </summary>
		/// <param name="createTaskRequest">The task creation request</param>
		/// <returns>The created task</returns>
		[HttpPost]
		[ProducesResponseType(typeof(TaskDto), 201)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequestDto createTaskRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var task = await taskService.CreateTaskAsync(createTaskRequest);
			return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
		}

		/// <summary>
		/// Update an existing task (cannot update completed tasks)
		/// </summary>
		/// <param name="id">The unique identifier of the task to update</param>
		/// <param name="updateTaskRequest">The task update request</param>
		/// <returns>The updated task</returns>
		[HttpPut("{id:guid}")]
		[ProducesResponseType(typeof(TaskDto), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(404)]
		[ProducesResponseType(409)]
		public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequestDto updateTaskRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				var task = await taskService.UpdateTaskAsync(id, updateTaskRequest);
				return task == null ? NotFound() : Ok(task);
			}
			catch (InvalidOperationException ex)
			{
				// Return 409 Conflict for completed tasks
				return Conflict(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Delete a task (cannot delete completed tasks)
		/// </summary>
		/// <param name="id">The unique identifier of the task to delete</param>
		/// <returns>No content if successful</returns>
		[HttpDelete("{id:guid}")]
		[ProducesResponseType(204)]
		[ProducesResponseType(404)]
		[ProducesResponseType(409)]
		public async Task<IActionResult> DeleteTask(Guid id)
		{
			try
			{
				var success = await taskService.DeleteTaskAsync(id);
				return !success ? NotFound() : NoContent();
			}
			catch (InvalidOperationException ex)
			{
				// Return 409 Conflict for completed tasks
				return Conflict(new { message = ex.Message });
			}
		}
	}
}
