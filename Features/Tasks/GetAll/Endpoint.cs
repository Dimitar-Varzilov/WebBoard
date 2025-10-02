using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Data;
using WebBoard.Features.Tasks.Create;

namespace WebBoard.Features.Tasks.GetAll
{
	public class GetAllTasksEndpoint(AppDbContext db) : EndpointWithoutRequest<List<TaskResponse>>
	{
		public override void Configure()
		{
			Get("/api/tasks");
			AllowAnonymous();
			Description(b => b
				.WithName("GetAllTasks")
				.Produces<List<TaskResponse>>(200));
		}

		public override async Task HandleAsync(CancellationToken ct)
		{
			var tasks = await db.Tasks
				.Select(t => new TaskResponse(t.Id, t.Title, t.Description, t.Status, t.CreatedAt))
				.ToListAsync(ct);

			await Send.OkAsync(tasks, ct);
		}
	}
}
