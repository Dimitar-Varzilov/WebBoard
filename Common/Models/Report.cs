using WebBoard.Common.Enums;

namespace WebBoard.Common.Models
{
	public record Report(
		Guid Id,
		Guid JobId,
		string FileName,
		string Content,
		string ContentType,
		DateTime CreatedAt,
		ReportStatus Status)
	{
		public Job? Job { get; init; }
	}
}