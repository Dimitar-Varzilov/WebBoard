using FastEndpoints;
using Microsoft.EntityFrameworkCore;
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
            Tags(Constants.SwaggerTags.Jobs);
            Summary(s =>
            {
                s.Summary = "Create a new background job";
                s.Description = "Creates and queues a new background job for a specified list of tasks.";
                s.Response<JobResponse>(201, "Job created and queued successfully.");
                s.Response(400, "Invalid request format or unknown job type.");
            });
        }

        public override async Task HandleAsync(CreateJobRequest req, CancellationToken ct)
        {
            var tasks = await db.Tasks
                .Where(t => req.TaskIds.Contains(t.Id))
                .ToListAsync(ct);

            if (tasks.Count != req.TaskIds.Count)
            {
                ThrowError("One or more specified tasks were not found.", StatusCodes.Status400BadRequest);
                return;
            }

            var job = new Job(Guid.NewGuid(), req.JobType, JobStatus.Queued, DateTime.UtcNow)
            {
                Tasks = tasks
            };

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
                new GetJobByIdRequest(job.Id),
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
