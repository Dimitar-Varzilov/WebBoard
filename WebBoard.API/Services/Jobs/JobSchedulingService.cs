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
		/// <returns>A configured ITrigger instance</returns>
		private ITrigger CreateJobTrigger(Guid jobId, DateTimeOffset? scheduledAt)
		{
			var triggerKey = new TriggerKey($"{jobId}-trigger");

			// Run immediately if no scheduled time
			if (scheduledAt == null)
			{
				return TriggerBuilder.Create()
					.WithIdentity(triggerKey)
					.StartNow()
					.Build();
			}

			// Check if scheduled time is in the past
			if (scheduledAt.Value <= DateTimeOffset.UtcNow)
			{
				logger.LogWarning("Job {JobId} scheduled time {ScheduledTime} is in the past, scheduling to run immediately",
					jobId, scheduledAt.Value);

				return TriggerBuilder.Create()
					.WithIdentity(triggerKey)
					.StartNow()
					.Build();
			}

			// Schedule for specific time
			return TriggerBuilder.Create()
				.WithIdentity(triggerKey)
				.StartAt(scheduledAt.Value)
				.Build();
		}
	}
}