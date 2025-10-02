namespace WebBoard.Features.Jobs.GetById
{
	public record GetJobByIdRequest(Guid Id)
	{
		public static ValueTask<GetJobByIdRequest?> BindAsync(HttpContext context)
		{
			var id = context.Request.RouteValues["id"]?.ToString();
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid parsedId))
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return ValueTask.FromResult<GetJobByIdRequest?>(null);
			}
			return ValueTask.FromResult<GetJobByIdRequest?>(new GetJobByIdRequest(parsedId));
		}
	}
}
