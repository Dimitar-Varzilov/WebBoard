using WebBoard.Common.Enums;

namespace WebBoard.Common.DTOs.Reports
{
	public record ReportDto(
		Guid Id,
		Guid JobId,
		string FileName,
		string ContentType,
		DateTime CreatedAt,
		ReportStatus Status);

	public record ReportDownloadDto(
		string FileName,
		string Content,
		string ContentType);
}