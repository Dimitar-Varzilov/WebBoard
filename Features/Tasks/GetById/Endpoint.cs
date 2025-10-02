using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Common;
using WebBoard.Data;
using WebBoard.Features.Tasks.Create;

namespace WebBoard.Features.Tasks.Get
{
    public class GetTaskByIdEndpoint(AppDbContext db) : EndpointWithoutRequest<TaskResponse>
    {
        public override void Configure()
        {
            Get(Constants.ApiRoutes.TaskById);
            AllowAnonymous();
            Tags(Constants.SwaggerTags.Tasks); // Add tag for grouping
            Summary(s =>
            {
                s.Summary = "Get a task by ID";
                s.Description = "Retrieves a specific task by its unique identifier";
                s.Params["id"] = "The unique identifier (GUID) of the task";
                s.Response<TaskResponse>(200, "Task found successfully");
                s.Response(404, "Task not found");
                s.Response(400, "Invalid task ID format");
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var id = Route<Guid>("id");

            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

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