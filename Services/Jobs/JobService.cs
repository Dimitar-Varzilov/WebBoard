using Microsoft.EntityFrameworkCore;
using WebBoard.Common.DTOs.Jobs;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	public class JobService(AppDbContext db, IJobSchedulingService jobSchedulingService) : IJobService
	{
		public async Task<IEnumerable<JobDto>> GetAllJobsAsync()
		{
			var jobs = await db.Jobs.AsNoTracking()
								.OrderByDescending(j => j.CreatedAt)
								.ToListAsync();

			return jobs.Select(job => new JobDto(job.Id, job.JobType, job.Status, job.CreatedAt, job.ScheduledAt));
		}

		public async Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest)
		{
			var scheduledAt = createJobRequest.RunImmediately ? null : createJobRequest.ScheduledAt;

			var job = new Job(
				Guid.NewGuid(),
				createJobRequest.JobType,
				JobStatus.Queued,
				DateTime.UtcNow,
				scheduledAt
			);

			db.Jobs.Add(job);
			await db.SaveChangesAsync();

			// Schedule the job immediately after creation
			await jobSchedulingService.ScheduleJobAsync(job);

			return new JobDto(job.Id, job.JobType, job.Status, job.CreatedAt, job.ScheduledAt);
		}

		public async Task<JobDto?> GetJobByIdAsync(Guid id)
		{
			var job = await db.Jobs.AsNoTracking()
								.FirstOrDefaultAsync(j => j.Id == id);

			return job == null ? null : new JobDto(job.Id, job.JobType, job.Status, job.CreatedAt, job.ScheduledAt);
		}
	}
}
