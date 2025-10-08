using Microsoft.AspNetCore.Mvc;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Jobs;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Tasks;

namespace WebBoard.API.Controllers
{
	[ApiController]
	[Route("api/jobs")]
	[Tags(Constants.SwaggerTags.Jobs)]
	public class JobsController(IJobService jobService, ITaskService taskService) : ControllerBase
	{
		/// <summary>
		/// Get jobs with pagination, filtering, and sorting
		/// </summary>
		/// <param name="parameters">Query parameters for pagination, filtering, and sorting</param>
		/// <returns>A paginated list of jobs with metadata</returns>
		[HttpGet]
		[ProducesResponseType(typeof(PagedResult<JobDto>), 200)]
		public async Task<IActionResult> GetJobs([FromQuery] JobQueryParameters parameters)
		{
			var result = await jobService.GetJobsAsync(parameters);
			return Ok(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(JobDto), 200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> GetJobById(Guid id)
		{
			var job = await jobService.GetJobByIdAsync(id);
			return job == null ? NotFound() : Ok(job);
		}

		/// <summary>
		/// Get count of pending tasks for job creation validation
		/// </summary>
		/// <returns>Count of pending tasks</returns>
		[HttpGet("validation/pending-tasks-count")]
		[ProducesResponseType(typeof(int), 200)]
		public async Task<IActionResult> GetPendingTasksCount()
		{
			var count = await taskService.GetTaskCountByStatusAsync(TaskItemStatus.Pending);
			return Ok(count);
		}

		/// <summary>
		/// Get available tasks for job creation (tasks not already assigned to other jobs)
		/// </summary>
		/// <param name="jobType">Job type to filter appropriate tasks</param>
		/// <returns>List of available tasks</returns>
		[HttpGet("validation/available-tasks")]
		[ProducesResponseType(typeof(IEnumerable<object>), 200)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> GetAvailableTasksForJob([FromQuery] string jobType)
		{
			if (string.IsNullOrWhiteSpace(jobType))
			{
				return BadRequest(new { message = "Job type is required" });
			}

			IEnumerable<TaskDto> availableTasks;

			// Filter tasks based on job type requirements
			if (jobType == Constants.JobTypes.MarkAllTasksAsDone)
			{
				// Only pending tasks that are not assigned to other jobs
				var pendingTasks = await taskService.GetTasksByStatusAsync(TaskItemStatus.Pending);
				availableTasks = pendingTasks
					.Where(t => !HasJobAssignment(t));
			}
			else
			{
				// All tasks not assigned to other jobs - use paginated query with large page size
				var parameters = new TaskQueryParameters
				{
					PageSize = 1000, // Large page size for available tasks
					HasJob = false, // Only tasks without job assignment
					SortBy = "CreatedAt",
					SortDirection = "desc"
				};
				var result = await taskService.GetTasksAsync(parameters);
				availableTasks = result.Items;
			}

			return Ok(availableTasks);
		}

		/// <summary>
		/// Create a new job
		/// </summary>
		/// <param name="createJobRequest">The job creation request</param>
		/// <returns>The created job</returns>
		[HttpPost]
		[ProducesResponseType(typeof(JobDto), 201)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> CreateJob([FromBody] CreateJobRequestDto createJobRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				var job = await jobService.CreateJobAsync(createJobRequest);
				return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, job);
			}
			catch (ArgumentException ex)
			{
				// Job type validation error or task selection error
				return BadRequest(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				// Business rule validation error (e.g., no pending tasks)
				return BadRequest(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Update an existing job (only queued jobs can be updated)
		/// </summary>
		/// <param name="id">The unique identifier of the job to update</param>
		/// <param name="updateJobRequest">The job update request</param>
		/// <returns>The updated job</returns>
		[HttpPut("{id:guid}")]
		[ProducesResponseType(typeof(JobDto), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(404)]
		[ProducesResponseType(409)]
		public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequestDto updateJobRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				var job = await jobService.UpdateJobAsync(id, updateJobRequest);
				return job == null ? NotFound() : Ok(job);
			}
			catch (InvalidOperationException ex)
			{
				// Return 409 Conflict for non-queued jobs
				return Conflict(new { message = ex.Message });
			}
			catch (ArgumentException ex)
			{
				// Job type validation error or task selection error
				return BadRequest(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Delete a job (only queued jobs can be deleted)
		/// </summary>
		[HttpDelete("{id:guid}")]
		[ProducesResponseType(204)]
		[ProducesResponseType(404)]
		[ProducesResponseType(409)]
		public async Task<IActionResult> DeleteJob(Guid id)
		{
			try
			{
				var deleted = await jobService.DeleteJobAsync(id);
				return deleted ? NoContent() : NotFound();
			}
			catch (InvalidOperationException ex)
			{
				// Return 409 Conflict for non-queued jobs
				return Conflict(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Helper method to check if a task is assigned to a job
		/// Checks if the task has a JobId indicating it's already assigned
		/// </summary>
		private static bool HasJobAssignment(TaskDto task)
		{
			return task.JobId.HasValue;
		}
	}
}
