using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Common;
using WebBoard.Data;
using WebBoard.Features.Tasks.Create;

namespace WebBoard.Features.Tasks.GetAll
{
	public class GetAllTasksEndpoint(AppDbContext db) : EndpointWithoutRequest<List<TaskResponse>>
	{
		public override void Configure()
		{
			Get(Constants.ApiRoutes.Tasks);
			AllowAnonymous();
			Tags(Constants.SwaggerTags.Tasks); // Add tag for grouping
			Summary(s =>
			{
				s.Summary = "Get all tasks";
				s.Description = "Retrieves a list of all tasks.";
				s.Response<List<TaskResponse>>(200, "Tasks retrieved successfully.");
			});
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
