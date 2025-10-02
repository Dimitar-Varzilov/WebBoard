using FastEndpoints;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;
using WebBoard.Features.Tasks.Get;

namespace WebBoard.Features.Tasks.Create
{
	public class CreateTaskEndpoint(AppDbContext db) : Endpoint<CreateTaskRequest, TaskResponse>
	{
		public override void Configure()
		{
			Post("/api/tasks");
			AllowAnonymous();
			Description(b => b
				.WithName("CreateTask")
				.Produces<TaskResponse>(201)
				.ProducesProblemFE(400));
		}

		public override async Task HandleAsync(CreateTaskRequest req, CancellationToken ct)
		{
			var task = new TaskItem(Guid.NewGuid(), DateTime.UtcNow, req.Title, req.Description, TaskItemStatus.Pending);

			db.Tasks.Add(task);
			await db.SaveChangesAsync(ct);

			await Send.CreatedAtAsync<GetTaskEndpoint>(
				new { id = task.Id },
				new TaskResponse(task.Id, task.Title, task.Description, task.Status, task.CreatedAt),
				cancellation: ct);
		}
	}
}
