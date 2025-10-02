using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Data;
using WebBoard.Features.Jobs.Create;

namespace WebBoard.Features.Jobs.Get
{
	public class GetJobEndpoint(AppDbContext db) : Endpoint<GetJobRequest, JobResponse>
	{
		public override void Configure()
		{
			Get("/api/jobs/{id}");
			AllowAnonymous();
			Description(b => b
				.Produces<JobResponse>(200)
				.ProducesProblemFE(404));
		}

		public override async Task HandleAsync(GetJobRequest req, CancellationToken ct)
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