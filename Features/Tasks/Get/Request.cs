namespace WebBoard.Features.Tasks.Get
{
	public record GetTaskRequest(Guid Id)
	{
		public static ValueTask<GetTaskRequest?> BindAsync(HttpContext context)
		{
			var id = context.Request.RouteValues["id"]?.ToString();
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid parsedId))
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return ValueTask.FromResult<GetTaskRequest?>(null);
			}
			return ValueTask.FromResult<GetTaskRequest?>(new GetTaskRequest(parsedId));
		}
	}
}