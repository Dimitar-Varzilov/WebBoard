using Microsoft.EntityFrameworkCore;
using WebBoard.Common.Constants;
using WebBoard.Common.DTOs.Jobs;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;
using WebBoard.Services.Tasks;

namespace WebBoard.Services.Jobs
{
	public class JobService(
		AppDbContext db,
		IJobSchedulingService jobSchedulingService,
		IJobTypeRegistry jobTypeRegistry,
		ITaskService taskService) : IJobService
	{
		public async Task<IEnumerable<JobDto>> GetAllJobsAsync()
		{
			var jobs = await db.Jobs
				.AsNoTracking()
				.Include(j => j.Report)
				.OrderByDescending(j => j.CreatedAt)
				.ToListAsync();

			return jobs.Select(job => new JobDto(
				job.Id,
				job.JobType,
				job.Status,
				job.CreatedAt,
				job.ScheduledAt,
				job.Report != null,
				job.Report?.Id,
				job.Report?.FileName));
		}

		public async Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest)
		{
			// 1. Validate job type exists
			if (!jobTypeRegistry.IsValidJobType(createJobRequest.JobType))
			{
				throw new ArgumentException($"Invalid job type: '{createJobRequest.JobType}'. Available types: {string.Join(", ", jobTypeRegistry.GetAllJobTypes())}");
			}

			// 2. Validate business rules (e.g., pending tasks requirement)
			await ValidateJobCreationRequirementsAsync(createJobRequest.JobType);

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
			var job = await db.Jobs
				.AsNoTracking()
				.Include(j => j.Report)
				.FirstOrDefaultAsync(j => j.Id == id);

			return job == null ? null : new JobDto(
				job.Id,
				job.JobType,
				job.Status,
				job.CreatedAt,
				job.ScheduledAt,
				job.Report != null,
				job.Report?.Id,
				job.Report?.FileName);
		}

		/// <summary>
		/// Validates all business requirements for job creation
		/// </summary>
		/// <param name="jobType">The job type to validate</param>
		/// <exception cref="InvalidOperationException">Thrown when business requirements are not met</exception>
		private async Task ValidateJobCreationRequirementsAsync(string jobType)
		{
			// Only MarkAllTasksAsDone requires pending tasks
			// GenerateTaskReport works with all tasks regardless of status
			if (RequiresPendingTasks(jobType))
			{
				var pendingTasksCount = await taskService.GetTaskCountByStatusAsync(TaskItemStatus.Pending);
				if (pendingTasksCount == 0)
				{
					throw new InvalidOperationException($"Cannot create job '{jobType}': No pending tasks available. This job requires at least one pending task to be created.");
				}
			}

			// Future business rules can be added here
			// e.g., user permissions, resource availability, scheduling conflicts, etc.
		}

		/// <summary>
		/// Determines if a job type requires pending tasks to be available
		/// Only MarkAllTasksAsDone requires pending tasks since it specifically works with pending tasks
		/// GenerateTaskReport can work with any tasks (pending, in-progress, completed)
		/// </summary>
		/// <param name="jobType">The job type to check</param>
		/// <returns>True if the job type requires pending tasks</returns>
		private static bool RequiresPendingTasks(string jobType)
		{
			return jobType switch
			{
				Constants.JobTypes.MarkAllTasksAsDone => true,
				Constants.JobTypes.GenerateTaskReport => false, // Can generate report even with no pending tasks
				_ => false
			};
		}
	}
}
