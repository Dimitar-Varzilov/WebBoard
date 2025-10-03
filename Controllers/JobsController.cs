using Microsoft.AspNetCore.Mvc;
using WebBoard.Common.DTOs.Jobs;
using WebBoard.Services.Jobs;

namespace WebBoard.Controllers
{
	[ApiController]
	[Route("api/jobs")]
	public class JobsController : ControllerBase
	{
		private readonly IJobService _jobService;

		public JobsController(IJobService jobService)
		{
			_jobService = jobService;
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetJobById(Guid id)
		{
			var job = await _jobService.GetJobByIdAsync(id);
			return job == null ? NotFound() : Ok(job);
		}

		[HttpPost]
		public async Task<IActionResult> CreateJob([FromBody] CreateJobRequestDto createJobRequest)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var job = await _jobService.CreateJobAsync(createJobRequest);
			return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, job);
		}
	}
}
