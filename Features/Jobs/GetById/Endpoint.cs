using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Data;
using WebBoard.Features.Jobs.Create;

namespace WebBoard.Features.Jobs.GetById
{
	public class GetJobByIdEndpoint(AppDbContext db) : Endpoint<GetJobByIdRequest, JobResponse>
	{
		public override void Configure()
		{
			Get("/api/jobs/{id:guid}");
			AllowAnonymous();
			Description(b => b
				.WithName("GetJobById")
				.Produces<JobResponse>(200)
				.ProducesProblemFE(404)
				.ProducesProblemFE(400));
		}

		public override async Task HandleAsync(GetJobByIdRequest req, CancellationToken ct)
		{
			var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == req.Id, ct);

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