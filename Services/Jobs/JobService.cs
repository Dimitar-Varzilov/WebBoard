using Microsoft.EntityFrameworkCore;
using WebBoard.Common.DTOs.Jobs;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	public class JobService(AppDbContext db) : IJobService
	{
		public async Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest)
		{
			var job = new Job(
				Guid.NewGuid(),
				createJobRequest.JobType,
				JobStatus.Queued,
				DateTime.UtcNow
			);

			db.Jobs.Add(job);
			await db.SaveChangesAsync();

			return new JobDto(job.Id, job.JobType, job.Status, job.CreatedAt);
		}

		public async Task<JobDto?> GetJobByIdAsync(Guid id)
		{
			var job = await db.Jobs.AsNoTracking()
								.FirstOrDefaultAsync(j => j.Id == id);

			return job == null ? null : new JobDto(job.Id, job.JobType, job.Status, job.CreatedAt);
		}
	}
}
