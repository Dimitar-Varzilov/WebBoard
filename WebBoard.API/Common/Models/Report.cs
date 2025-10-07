using WebBoard.API.Common.Enums;

namespace WebBoard.API.Common.Models
{
	public record Report(
		Guid Id,
		Guid JobId,
		string FileName,
		string Content,
		string ContentType,
		DateTimeOffset CreatedAt,
		ReportStatus Status = ReportStatus.Generated)
	{
		public Job? Job { get; init; }
	}
}