namespace WebBoard.Features.Jobs.Get
{
	public record GetJobRequest(Guid Id)
	{
		public static ValueTask<GetJobRequest?> BindAsync(HttpContext context)
		{
			var id = context.Request.RouteValues["id"]?.ToString();
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid parsedId))
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return ValueTask.FromResult<GetJobRequest?>(null);
			}
			return ValueTask.FromResult<GetJobRequest?>(new GetJobRequest(parsedId));
		}
	}
}
