using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Common;
using WebBoard.Data;

namespace WebBoard.Features.Tasks.Delete
{
    public class DeleteTaskEndpoint(AppDbContext db) : Endpoint<DeleteTaskRequest>
    {
        public override void Configure()
        {
            Delete(Constants.ApiRoutes.TaskById);
            AllowAnonymous();
            Tags(Constants.SwaggerTags.Tasks);
            Summary(s =>
            {
                s.Summary = "Delete a task by ID";
                s.Description = "Deletes a specific task by its unique identifier.";
                s.Params["id"] = "The unique identifier of the task to delete.";
                s.Response(204, "Task deleted successfully.");
                s.Response(404, "Task not found.");
            });
        }

        public override async Task HandleAsync(DeleteTaskRequest req, CancellationToken ct)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == req.Id, ct);

            if (task == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            db.Tasks.Remove(task);
            await db.SaveChangesAsync(ct);

            await Send.NoContentAsync(ct);
        }
    }
}
