using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Common;
using WebBoard.Data;
using WebBoard.Features.Jobs.Create;

namespace WebBoard.Features.Jobs.GetById
{
	public class GetJobByIdEndpoint(AppDbContext db) : EndpointWithoutRequest<JobResponse>
	{
		public override void Configure()
		{
			Get(Constants.ApiRoutes.JobById);
			AllowAnonymous();
			Tags(Constants.SwaggerTags.Jobs); // Add tag for grouping
			Summary(s =>
			{
				s.Summary = "Get a job by ID";
				s.Description = "Retrieves a specific job by its unique identifier";
				s.Params["id"] = "The unique identifier (GUID) of the job";
				s.Response<JobResponse>(200, "Job found successfully");
				s.Response(404, "Job not found");
				s.Response(400, "Invalid job ID format");
			});
		}

		public override async Task HandleAsync(CancellationToken ct)
		{
			var id = Route<Guid>("id");

			var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);

			if (job == null)
			{
				await Send.NotFoundAsync(ct);
				return;
			}

			var response = new JobResponse(job.Id, job.JobType, job.Status.ToString(), job.CreatedAt);
			await Send.OkAsync(response, ct);
		}
	}
}