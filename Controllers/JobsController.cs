using Microsoft.AspNetCore.Mvc;
using WebBoard.Common.Constants;
using WebBoard.Common.DTOs.Jobs;
using WebBoard.Common.Enums;
using WebBoard.Services.Jobs;
using WebBoard.Services.Tasks;

namespace WebBoard.Controllers
{
	[ApiController]
	[Route("api/jobs")]
	[Tags(Constants.SwaggerTags.Jobs)]
	public class JobsController(IJobService jobService, ITaskService taskService) : ControllerBase
	{
		[HttpGet]
		[ProducesResponseType(typeof(IEnumerable<JobDto>), 200)]
		public async Task<IActionResult> GetAllJobs()
		{
			var jobs = await jobService.GetAllJobsAsync();
			return Ok(jobs);
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
				// Job type validation error
				return BadRequest(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				// Business rule validation error (e.g., no pending tasks)
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
