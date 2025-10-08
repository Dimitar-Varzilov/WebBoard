using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Jobs;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Extensions;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;

namespace WebBoard.API.Services.Jobs
{
	public class JobService(
		AppDbContext db,
		IJobSchedulingService jobSchedulingService,
		IJobTypeRegistry jobTypeRegistry) : IJobService
	{
		public async Task<PagedResult<JobDto>> GetJobsAsync(JobQueryParameters parameters)
		{
			var query = db.Jobs
				.AsNoTracking()
				.Include(j => j.Report)
				.Include(j => j.Tasks)
				.AsQueryable();

			// Apply filtering
			if (parameters.Status.HasValue)
			{
				query = query.Where(j => (int)j.Status == parameters.Status.Value);
			}

			if (!string.IsNullOrWhiteSpace(parameters.JobType))
			{
				query = query.Where(j => j.JobType == parameters.JobType);
			}

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm;

                // IMPORTANT: When adding new searchable string fields to the Job model,
                // add them to this search query to ensure they're included in search results.
                // Current searchable fields: JobType
                // Example: For a new Description field, add: j => j.Description
                query = query.SearchInNullableFields(searchTerm, [
                    j => j.JobType
                ]);
				// Add new searchable fields here when Job model is extended
			}

			// Apply sorting
			query = query.ApplySort(parameters.SortBy ?? "CreatedAt", parameters.IsAscending);

			// Get total count before pagination
			var totalCount = await query.CountAsync();

			// Apply pagination
			var jobs = await query
				.ApplyPagination(parameters)
				.ToListAsync();

			// Project to DTOs
			var jobDtos = jobs.Select(job => new JobDto(
				job.Id,
				job.JobType,
				job.Status,
				job.CreatedAt,
				job.ScheduledAt,
				job.Report != null,
				job.Report?.Id,
				job.Report?.FileName,
				job.Tasks.Select(t => t.Id)));

			return new PagedResult<JobDto>(jobDtos, totalCount, parameters.PageNumber, parameters.PageSize);
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
				// Convert to UTC for validation (if not already)
				var scheduledAtUtc = createJobRequest.ScheduledAt.Value.ToUniversalTime();
				if (scheduledAtUtc <= DateTimeOffset.UtcNow)
				{
					throw new ArgumentException("Scheduled time cannot be in the past.");
				}
			}

			// 5. Validate business rules (e.g., pending tasks requirement)
			await ValidateJobCreationRequirementsAsync(createJobRequest.JobType, createJobRequest.TaskIds);

			// ? FIX: Convert ScheduledAt to UTC to prevent PostgreSQL timezone error
			// PostgreSQL only accepts DateTimeOffset with UTC offset (00:00:00)
			var scheduledAt = createJobRequest.RunImmediately 
				? null 
				: createJobRequest.ScheduledAt?.ToUniversalTime();

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

		public async Task<JobDto?> UpdateJobAsync(Guid id, UpdateJobRequestDto updateJobRequest)
		{
			var job = await db.Jobs
				.Include(j => j.Tasks)
				.FirstOrDefaultAsync(j => j.Id == id);

			if (job == null)
			{
				return null;
			}

			// ? Prevent editing non-queued jobs (Running, Completed, Failed)
			if (job.Status != JobStatus.Queued)
			{
				throw new InvalidOperationException($"Cannot update a {job.Status.ToString().ToLower()} job. Only queued jobs can be edited.");
			}

			// 1. Validate job type exists
			if (!jobTypeRegistry.IsValidJobType(updateJobRequest.JobType))
			{
				throw new ArgumentException($"Invalid job type: '{updateJobRequest.JobType}'. Available types: {string.Join(", ", jobTypeRegistry.GetAllJobTypes())}");
			}

			// 2. Validate task selection
			if (updateJobRequest.TaskIds == null || !updateJobRequest.TaskIds.Any())
			{
				throw new ArgumentException("At least one task must be selected for job processing.");
			}

			// 3. Validate selected tasks exist and have correct status
			await ValidateSelectedTasksAsync(updateJobRequest.TaskIds, updateJobRequest.JobType, job.Id);

			// 4. Validate scheduling time is not in the past
			if (!updateJobRequest.RunImmediately && updateJobRequest.ScheduledAt.HasValue)
			{
				// Convert to UTC for validation (if not already)
				var scheduledAtUtc = updateJobRequest.ScheduledAt.Value.ToUniversalTime();
				if (scheduledAtUtc <= DateTimeOffset.UtcNow)
				{
					throw new ArgumentException("Scheduled time cannot be in the past.");
				}
			}

			// 5. Validate business rules
			await ValidateJobCreationRequirementsAsync(updateJobRequest.JobType, updateJobRequest.TaskIds);

			// ? FIX: Convert ScheduledAt to UTC to prevent PostgreSQL timezone error
			// PostgreSQL only accepts DateTimeOffset with UTC offset (00:00:00)
			var scheduledAt = updateJobRequest.RunImmediately 
				? null 
				: updateJobRequest.ScheduledAt?.ToUniversalTime();

			// Unassign previous tasks from this job
			await UnassignTasksFromJobAsync(job.Id);

			// Update job properties
			var updatedJob = job with
			{
				JobType = updateJobRequest.JobType,
				ScheduledAt = scheduledAt
			};

			db.Entry(job).CurrentValues.SetValues(updatedJob);

			// Assign new tasks to the job
			await AssignTasksToJobAsync(job.Id, updateJobRequest.TaskIds);

			await db.SaveChangesAsync();

			// Reschedule the job with new parameters (pass full job object)
			await jobSchedulingService.RescheduleJobAsync(updatedJob);

			return new JobDto(
				updatedJob.Id,
				updatedJob.JobType,
				updatedJob.Status,
				updatedJob.CreatedAt,
				updatedJob.ScheduledAt,
				false,
				null,
				null,
				updateJobRequest.TaskIds);
		}

		public async Task<bool> DeleteJobAsync(Guid id)
		{
			var job = await db.Jobs
				.Include(j => j.Tasks)
				.FirstOrDefaultAsync(j => j.Id == id);

			if (job == null)
			{
				return false;
			}

			// ? Prevent deleting non-queued jobs (Running, Completed, Failed)
			if (job.Status != JobStatus.Queued)
			{
				throw new InvalidOperationException($"Cannot delete a {job.Status.ToString().ToLower()} job. Only queued jobs can be deleted.");
			}

			// Unassign all tasks from this job
			await UnassignTasksFromJobAsync(job.Id);

			// Remove the job from database
			db.Jobs.Remove(job);

			await db.SaveChangesAsync();

			// Remove the job from Quartz scheduler
			await jobSchedulingService.UnscheduleJobAsync(job.Id);

			return true;
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
		/// <param name="excludeJobId">Job ID to exclude from job assignment check (for updates)</param>
		/// <exception cref="ArgumentException">Thrown when validation fails</exception>
		private async Task ValidateSelectedTasksAsync(IEnumerable<Guid> taskIds, string jobType, Guid? excludeJobId = null)
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

			// Check if any selected tasks are already assigned to another job (excluding current job when updating)
			var tasksWithJobs = excludeJobId.HasValue
				? existingTasks.Where(t => t.JobId.HasValue && t.JobId != excludeJobId).ToList()
				: existingTasks.Where(t => t.JobId.HasValue).ToList();

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
		/// Unassigns all tasks from the job
		/// </summary>
		/// <param name="jobId">Job ID</param>
		private async Task UnassignTasksFromJobAsync(Guid jobId)
		{
			var tasks = await db.Tasks
				.Where(t => t.JobId == jobId)
				.ToListAsync();

			foreach (var task in tasks)
			{
				var updatedTask = task with { JobId = null };
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
