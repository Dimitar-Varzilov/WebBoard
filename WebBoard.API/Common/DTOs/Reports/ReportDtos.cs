using WebBoard.API.Common.Enums;

namespace WebBoard.API.Common.DTOs.Reports
{
	public record ReportDto(
		Guid Id,
		Guid JobId,
		string FileName,
		string ContentType,
		DateTimeOffset CreatedAt,
		ReportStatus Status);

	public record ReportDownloadDto(
		string FileName,
		string Content,
		string ContentType);
}