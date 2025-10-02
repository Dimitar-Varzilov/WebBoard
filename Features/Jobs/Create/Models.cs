namespace WebBoard.Features.Jobs.Create
{
	public record CreateJobRequest(string JobType);

	public record JobResponse(Guid Id, string JobType, string Status, DateTime CreatedAt);
}