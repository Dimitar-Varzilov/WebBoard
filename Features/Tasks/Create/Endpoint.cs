using FastEndpoints;
using WebBoard.Common;
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
			Post(Constants.ApiRoutes.Tasks);
			AllowAnonymous();
			Tags(Constants.SwaggerTags.Tasks); // Add tag for grouping
			Description(b => b
				.WithName("CreateTask")
				.Produces<TaskResponse>(201)
				.ProducesProblemFE(400));
			Summary(s =>
			{
				s.Summary = "Create a new task";
				s.Description = "Creates a new task with a title and description.";
				s.Response<TaskResponse>(201, "Task created successfully.");
				s.Response(400, "Invalid request format.");
			});
		}

		public override async Task HandleAsync(CreateTaskRequest req, CancellationToken ct)
		{
			var task = new TaskItem(Guid.NewGuid(), DateTime.UtcNow, req.Title, req.Description, TaskItemStatus.Pending, null);

			db.Tasks.Add(task);
			await db.SaveChangesAsync(ct);

			var response = new TaskResponse(task.Id, task.Title, task.Description, task.Status, task.CreatedAt);

			await Send.CreatedAtAsync<GetTaskByIdEndpoint>(
				new GetTaskByIdRequest(task.Id),
				response,
				cancellation: ct);
		}
	}
}
