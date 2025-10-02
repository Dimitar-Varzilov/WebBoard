using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using WebBoard.Common;
using WebBoard.Data;

namespace WebBoard.Features.Tasks.Update
{
    public class UpdateTaskEndpoint(AppDbContext db) : Endpoint<UpdateTaskRequest, UpdateTaskResponse>
    {
        public override void Configure()
        {
            Put(Constants.ApiRoutes.TaskById);
            AllowAnonymous();
            Tags(Constants.SwaggerTags.Tasks);
            Summary(s =>
            {
                s.Summary = "Update a task by ID";
                s.Description = "Updates a specific task by its unique identifier.";
                s.Params["id"] = "The unique identifier of the task to update.";
                s.Response<UpdateTaskResponse>(200, "Task updated successfully.");
                s.Response(404, "Task not found.");
            });
        }

        public override async Task HandleAsync(UpdateTaskRequest req, CancellationToken ct)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == req.Id, ct);

            if (task == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            var updatedTask = task with
            {
                Title = req.Title,
                Description = req.Description,
                Status = req.Status
            };

            db.Entry(task).CurrentValues.SetValues(updatedTask);
            await db.SaveChangesAsync(ct);

            var response = new UpdateTaskResponse(updatedTask.Id, updatedTask.Title, updatedTask.Description, updatedTask.Status, updatedTask.CreatedAt);
            await Send.OkAsync(response, ct);
        }
    }
}
