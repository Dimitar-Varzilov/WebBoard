using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Data;
using WebBoard.Features.Tasks.Create;

namespace WebBoard.Features.Tasks.Get
{
	public class GetTaskEndpoint(AppDbContext db) : Endpoint<GetTaskRequest, TaskResponse>
	{
		public override void Configure()
		{
			Get("/api/tasks/{id:guid}");
			AllowAnonymous();
			Description(b => b
				.WithName("GetTaskById")
				.Produces<TaskResponse>(200)
				.ProducesProblemFE(404)
				.ProducesProblemFE(400));
		}

		public override async Task HandleAsync(GetTaskRequest req, CancellationToken ct)
		{
			var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == req.Id, ct);

			if (task == null)
			{
				await Send.NotFoundAsync(ct);
				return;
			}

			var response = new TaskResponse(task.Id, task.Title, task.Description, task.Status, task.CreatedAt);
			await Send.OkAsync(response, ct);
		}
	}
}