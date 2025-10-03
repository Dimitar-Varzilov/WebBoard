using Quartz;
using WebBoard.Common.Constants;
using WebBoard.Common.Models;

namespace WebBoard.Services.Jobs
{
	public interface IJobSchedulingService
	{
		Task ScheduleJobAsync(Job job);
	}

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

				// Create job data map with job ID
				var jobDataMap = new JobDataMap
				{
					[Constants.JobDataKeys.JobId] = job.Id
				};

				// Get job type using the registry
				var quartzJobType = jobTypeRegistry.GetJobType(job.JobType);

				// Create job detail
				var jobDetail = JobBuilder.Create(quartzJobType)
					.WithIdentity(jobKey)
					.SetJobData(jobDataMap)
					.Build();

				// Create trigger based on scheduling
				ITrigger trigger;
				if (job.ScheduledAt == null)
				{
					// Run immediately
					trigger = TriggerBuilder.Create()
						.WithIdentity(triggerKey)
						.StartNow()
						.Build();
				}
				else
				{
					// Check if scheduled time is in the past
					if (job.ScheduledAt.Value <= DateTime.UtcNow)
					{
						logger.LogWarning("Job {JobId} scheduled time {ScheduledTime} is in the past, scheduling to run immediately",
							job.Id, job.ScheduledAt.Value);

						trigger = TriggerBuilder.Create()
							.WithIdentity(triggerKey)
							.StartNow()
							.Build();
					}
					else
					{
						// Schedule for specific time
						trigger = TriggerBuilder.Create()
							.WithIdentity(triggerKey)
							.StartAt(job.ScheduledAt.Value)
							.Build();
					}
				}

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
	}
}