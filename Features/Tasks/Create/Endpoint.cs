using FastEndpoints;
using WebBoard.Common;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Features.Tasks.Create
{
	public class CreateTaskEndpoint(AppDbContext db) : Endpoint<CreateTaskRequest, TaskResponse>
	{
		public override void Configure()
		{
			Post(Constants.ApiRoutes.Tasks);
			AllowAnonymous();
			Tags(Constants.SwaggerTags.Tasks); // Add tag for grouping
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

			// Send created response with location header
			var response = new TaskResponse(task.Id, task.Title, task.Description, task.Status, task.CreatedAt);
			await Send.CreatedAtAsync(
				Constants.ApiRoutes.TaskById.Replace("{id:guid}", task.Id.ToString()),
				response,
				cancellation: ct);
		}
	}
}
