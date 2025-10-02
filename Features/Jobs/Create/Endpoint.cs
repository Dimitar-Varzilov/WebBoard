using FastEndpoints;
using Quartz;
using WebBoard.Common;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;
using WebBoard.Features.Jobs.GetById;
using WebBoard.Services.Jobs;

namespace WebBoard.Features.Jobs.Create
{
	public class CreateJobEndpoint(AppDbContext db, IScheduler scheduler) : Endpoint<CreateJobRequest, JobResponse>
	{
		public override void Configure()
		{
			Post(Constants.ApiRoutes.Jobs);
			AllowAnonymous();
			Tags(Constants.SwaggerTags.Jobs); // Add tag for grouping
			Description(b => b
				.WithName("CreateJob")
				.Produces<JobResponse>(201)
				.ProducesProblemFE(400));
		}

		public override async Task HandleAsync(CreateJobRequest req, CancellationToken ct)
		{
			// Create job record in database
			var job = new Job(Guid.NewGuid(), req.JobType, JobStatus.Queued, DateTime.UtcNow);

			db.Jobs.Add(job);
			await db.SaveChangesAsync(ct);

			// Schedule the job in Quartz
			var jobDetail = JobBuilder.Create(GetJobType(req.JobType))
				.WithIdentity($"{req.JobType}-{job.Id}")
				.UsingJobData(Constants.JobDataKeys.JobId, job.Id.ToString())
				.Build();

			var trigger = TriggerBuilder.Create()
				.WithIdentity($"{req.JobType}-trigger-{job.Id}")
				.StartNow()
				.Build();

			await scheduler.ScheduleJob(jobDetail, trigger, ct);

			var response = new JobResponse(job.Id, job.JobType, job.Status.ToString(), job.CreatedAt);

			await Send.CreatedAtAsync<GetJobByIdEndpoint>(
				new { id = job.Id },
				response,
				cancellation: ct);
		}

		private static Type GetJobType(string jobType)
		{
			return jobType switch
			{
				Constants.JobTypes.MarkTasksAsCompleted => typeof(MarkTasksAsCompletedJob),
				Constants.JobTypes.GenerateTaskList => typeof(GenerateTaskListJob),
				_ => throw new ArgumentException($"Unknown job type: {jobType}")
			};
		}
	}
}
