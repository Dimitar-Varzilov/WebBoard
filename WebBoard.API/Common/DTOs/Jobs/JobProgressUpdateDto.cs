namespace WebBoard.API.Common.DTOs.Jobs
{
	/// <summary>
	/// DTO for job progress update notifications
	/// </summary>
	public record JobProgressUpdateDto(
		Guid JobId,
		int Progress,
		DateTimeOffset UpdatedAt);
}
