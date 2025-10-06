using Microsoft.EntityFrameworkCore;
using WebBoard.Common.Constants;
using WebBoard.Common.DTOs.Jobs;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Jobs
{
	public class JobService(
		AppDbContext db,
		IJobSchedulingService jobSchedulingService,
		IJobTypeRegistry jobTypeRegistry) : IJobService
	{
		public async Task<IEnumerable<JobDto>> GetAllJobsAsync()
		{
			var jobs = await db.Jobs
				.AsNoTracking()
				.Include(j => j.Report)
				.Include(j => j.Tasks)
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
				job.Report?.FileName,
				job.Tasks.Select(t => t.Id)));
		}

		public async Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest)
		{
			// 1. Validate job type exists
			if (!jobTypeRegistry.IsValidJobType(createJobRequest.JobType))
			{
				throw new ArgumentException($"Invalid job type: '{createJobRequest.JobType}'. Available types: {string.Join(", ", jobTypeRegistry.GetAllJobTypes())}");
			}

			// 2. Validate task selection
			if (createJobRequest.TaskIds == null || !createJobRequest.TaskIds.Any())
			{
				throw new ArgumentException("At least one task must be selected for job processing.");
			}

			// 3. Validate selected tasks exist and have correct status
			await ValidateSelectedTasksAsync(createJobRequest.TaskIds, createJobRequest.JobType);

			// 4. Validate scheduling time is not in the past
			if (!createJobRequest.RunImmediately && createJobRequest.ScheduledAt.HasValue)
			{
				if (createJobRequest.ScheduledAt.Value <= DateTimeOffset.UtcNow)
				{
					throw new ArgumentException("Scheduled time cannot be in the past.");
				}
			}

			// 5. Validate business rules (e.g., pending tasks requirement)
			await ValidateJobCreationRequirementsAsync(createJobRequest.JobType, createJobRequest.TaskIds);

			var scheduledAt = createJobRequest.RunImmediately ? null : createJobRequest.ScheduledAt;

			var job = new Job(
				Guid.NewGuid(),
				createJobRequest.JobType,
				JobStatus.Queued,
				DateTimeOffset.UtcNow,
				scheduledAt
			);

			db.Jobs.Add(job);

			// Associate tasks with the job
			await AssignTasksToJobAsync(job.Id, createJobRequest.TaskIds);

			await db.SaveChangesAsync();

			// Schedule the job immediately after creation
			await jobSchedulingService.ScheduleJobAsync(job);

			return new JobDto(job.Id, job.JobType, job.Status, job.CreatedAt, job.ScheduledAt, false, null, null, createJobRequest.TaskIds);
		}

		public async Task<JobDto?> GetJobByIdAsync(Guid id)
		{
			var job = await db.Jobs
				.AsNoTracking()
				.Include(j => j.Report)
				.Include(j => j.Tasks)
				.FirstOrDefaultAsync(j => j.Id == id);

			return job == null ? null : new JobDto(
				job.Id,
				job.JobType,
				job.Status,
				job.CreatedAt,
				job.ScheduledAt,
				job.Report != null,
				job.Report?.Id,
				job.Report?.FileName,
				job.Tasks.Select(t => t.Id));
		}

		/// <summary>
		/// Validates that selected tasks exist and are in the correct status for the job type
		/// </summary>
		/// <param name="taskIds">Selected task IDs</param>
		/// <param name="jobType">Job type being created</param>
		/// <exception cref="ArgumentException">Thrown when validation fails</exception>
		private async Task ValidateSelectedTasksAsync(IEnumerable<Guid> taskIds, string jobType)
		{
			var taskIdsList = taskIds.ToList();

			// Check if all selected tasks exist
			var existingTasks = await db.Tasks
				.Where(t => taskIdsList.Contains(t.Id))
				.ToListAsync();

			if (existingTasks.Count != taskIdsList.Count)
			{
				var missingTaskIds = taskIdsList.Except(existingTasks.Select(t => t.Id));
				throw new ArgumentException($"The following task IDs do not exist: {string.Join(", ", missingTaskIds)}");
			}

			// Validate task status based on job type
			if (jobType == Constants.JobTypes.MarkAllTasksAsDone)
			{
				var nonPendingTasks = existingTasks.Where(t => t.Status != TaskItemStatus.Pending).ToList();
				if (nonPendingTasks.Count != 0)
				{
					var nonPendingTitles = string.Join(", ", nonPendingTasks.Select(t => $"'{t.Title}'"));
					throw new ArgumentException($"'Mark All Tasks as Done' can only process pending tasks. The following selected tasks are not pending: {nonPendingTitles}");
				}
			}

			// Check if any selected tasks are already assigned to another job
			var tasksWithJobs = existingTasks.Where(t => t.JobId.HasValue).ToList();
			if (tasksWithJobs.Count != 0)
			{
				var taskTitles = string.Join(", ", tasksWithJobs.Select(t => $"'{t.Title}'"));
				throw new ArgumentException($"The following tasks are already assigned to another job: {taskTitles}");
			}
		}

		/// <summary>
		/// Assigns selected tasks to the job
		/// </summary>
		/// <param name="jobId">Job ID</param>
		/// <param name="taskIds">Task IDs to assign</param>
		private async Task AssignTasksToJobAsync(Guid jobId, IEnumerable<Guid> taskIds)
		{
			var tasks = await db.Tasks
				.Where(t => taskIds.Contains(t.Id))
				.ToListAsync();

			foreach (var task in tasks)
			{
				var updatedTask = task with { JobId = jobId };
				db.Entry(task).CurrentValues.SetValues(updatedTask);
			}
		}

		/// <summary>
		/// Validates all business requirements for job creation
		/// </summary>
		/// <param name="jobType">The job type to validate</param>
		/// <param name="taskIds">Selected task IDs</param>
		/// <exception cref="InvalidOperationException">Thrown when business requirements are not met</exception>
		private async Task ValidateJobCreationRequirementsAsync(string jobType, IEnumerable<Guid> taskIds)
		{
			// For MarkAllTasksAsDone, ensure we have pending tasks in the selection
			if (RequiresPendingTasks(jobType))
			{
				var selectedPendingCount = await db.Tasks
					.Where(t => taskIds.Contains(t.Id) && t.Status == TaskItemStatus.Pending)
					.CountAsync();

				if (selectedPendingCount == 0)
				{
					throw new InvalidOperationException($"Cannot create job '{jobType}': No pending tasks selected. This job requires at least one pending task to be selected.");
				}
			}

			// Future business rules can be added here
		}

		/// <summary>
		/// Determines if a job type requires pending tasks to be available
		/// </summary>
		/// <param name="jobType">The job type to check</param>
		/// <returns>True if the job type requires pending tasks</returns>
		private static bool RequiresPendingTasks(string jobType)
		{
			return jobType switch
			{
				Constants.JobTypes.MarkAllTasksAsDone => true,
				Constants.JobTypes.GenerateTaskReport => false, // Can generate report with any task status
				_ => false
			};
		}
	}
}
