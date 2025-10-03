using Microsoft.AspNetCore.Mvc;
using WebBoard.Common.DTOs.Tasks;
using WebBoard.Services.Tasks;

namespace WebBoard.Controllers
{
	[ApiController]
	[Route("api/tasks")]
	public class TasksController(ITaskService taskService) : ControllerBase
	{
		[HttpGet]
		[ProducesResponseType(typeof(IEnumerable<TaskDto>), 200)]
		public async Task<IActionResult> GetAllTasks()
		{
			var tasks = await taskService.GetAllTasksAsync();
			return Ok(tasks);
		}

		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(TaskDto), 200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> GetTaskById(Guid id)
		{
			var task = await taskService.GetTaskByIdAsync(id);
			return task == null ? NotFound() : Ok(task);
		}

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

		[HttpPut("{id:guid}")]
		[ProducesResponseType(typeof(TaskDto), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequestDto updateTaskRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var task = await taskService.UpdateTaskAsync(id, updateTaskRequest);
			return task == null ? NotFound() : Ok(task);
		}

		[HttpDelete("{id:guid}")]
		[ProducesResponseType(204)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> DeleteTask(Guid id)
		{
			var success = await taskService.DeleteTaskAsync(id);
			return !success ? NotFound() : NoContent();
		}
	}
}
