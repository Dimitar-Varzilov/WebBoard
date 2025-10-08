using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Services.Jobs
{
	public class JobSchedulingService(
		IScheduler scheduler,
		IJobTypeRegistry jobTypeRegistry,
		ILogger<JobSchedulingService> logger) : IJobSchedulingService
	{
		public async Task ScheduleJobAsync(Job job)
		{
			try
			{
				var jobKey = new JobKey(job.Id.ToString());
				var triggerKey = new TriggerKey($"{job.Id}-trigger");

				// Check if job is already scheduled and remove it first
				if (await scheduler.CheckExists(jobKey))
				{
					logger.LogInformation("Job {JobId} already exists in scheduler, removing and rescheduling", job.Id);
					await scheduler.DeleteJob(jobKey);
				}

				// Create job detail
				var jobDetail = CreateJobDetail(job.Id, job.JobType);

				// Create trigger based on scheduling
				var trigger = CreateJobTrigger(job.Id, job.ScheduledAt);

				// Schedule the job
				await scheduler.ScheduleJob(jobDetail, trigger);

				logger.LogInformation("Scheduled job {JobId} of type {JobType} to run at {ScheduledTime}",
					job.Id, job.JobType, job.ScheduledAt?.ToString() ?? "immediately");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error scheduling job {JobId}", job.Id);
				throw;
			}
		}

		public async Task RescheduleJobAsync(Job job)
		{
			try
			{
				var jobKey = new JobKey(job.Id.ToString());
				var oldTriggerKey = new TriggerKey($"{job.Id}-trigger");

				// Check if job exists in scheduler
				var jobExists = await scheduler.CheckExists(jobKey);

				if (jobExists)
				{
					// Remove existing job and its triggers (job type might have changed)
					logger.LogInformation("Job {JobId} exists in scheduler, removing it before rescheduling", job.Id);
					await scheduler.DeleteJob(jobKey);
				}

				// Recreate job detail (handles job type changes)
				var jobDetail = CreateJobDetail(job.Id, job.JobType);

				// Create new trigger with updated schedule
				var newTrigger = CreateJobTrigger(job.Id, job.ScheduledAt);

				// Schedule the job with the new job detail and trigger
				await scheduler.ScheduleJob(jobDetail, newTrigger);

				logger.LogInformation("Rescheduled job {JobId} of type {JobType} to run at {ScheduledTime}",
					job.Id, job.JobType, job.ScheduledAt?.ToString() ?? "immediately");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error rescheduling job {JobId}", job.Id);
				throw;
			}
		}

		/// <summary>
		/// Creates a Quartz JobDetail for the specified job
		/// </summary>
		/// <param name="jobId">The unique identifier for the job</param>
		/// <param name="jobType">The type of job to create</param>
		/// <returns>A configured IJobDetail instance</returns>
		private IJobDetail CreateJobDetail(Guid jobId, string jobType)
		{
			var jobKey = new JobKey(jobId.ToString());

			// Create job data map with job ID
			var jobDataMap = new JobDataMap
			{
				[Constants.JobDataKeys.JobId] = jobId
			};

			// Get job type using the registry
			var quartzJobType = jobTypeRegistry.GetJobType(jobType);

			// Create and return job detail
			return JobBuilder.Create(quartzJobType)
				.WithIdentity(jobKey)
				.SetJobData(jobDataMap)
				.Build();
		}

		/// <summary>
		/// Creates a Quartz trigger for the specified job
		/// </summary>
		/// <param name="jobId">The unique identifier for the job</param>
		/// <param name="scheduledAt">The scheduled execution time, or null to run immediately</param>
		/// <param name="jobKey">Optional job key to associate with the trigger (used for rescheduling)</param>
		/// <returns>A configured ITrigger instance</returns>
		private ITrigger CreateJobTrigger(Guid jobId, DateTimeOffset? scheduledAt, JobKey? jobKey = null)
		{
			var triggerKey = new TriggerKey($"{jobId}-trigger");

			// Build trigger with optional job key association
			var triggerBuilder = TriggerBuilder.Create()
				.WithIdentity(triggerKey);

			// If jobKey is provided, associate trigger with the job (for rescheduling)
			if (jobKey != null)
			{
				triggerBuilder.ForJob(jobKey);
			}

			// Run immediately if no scheduled time
			if (scheduledAt == null)
			{
				return triggerBuilder.StartNow().Build();
			}

			// Check if scheduled time is in the past
			if (scheduledAt.Value <= DateTimeOffset.UtcNow)
			{
				logger.LogWarning("Job {JobId} scheduled time {ScheduledTime} is in the past, scheduling to run immediately",
					jobId, scheduledAt.Value);

				return triggerBuilder.StartNow().Build();
			}

			// Schedule for specific time
			return triggerBuilder.StartAt(scheduledAt.Value).Build();
		}
	}
}