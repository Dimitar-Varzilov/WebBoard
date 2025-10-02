namespace WebBoard.Features.Jobs.Create
{
	public record JobResponse(Guid Id, string JobType, string Status, DateTime CreatedAt);
}
