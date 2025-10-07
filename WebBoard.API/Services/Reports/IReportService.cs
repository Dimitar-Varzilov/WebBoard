using WebBoard.API.Common.DTOs.Reports;
using WebBoard.API.Common.Models;

namespace WebBoard.API.Services.Reports
{
	public interface IReportService
	{
		Task<Report> CreateReportAsync(Guid jobId, string fileName, string content, string contentType);
		Task<ReportDownloadDto?> GetReportForDownloadAsync(Guid reportId);
		Task<ReportDto?> GetReportByJobIdAsync(Guid jobId);
		Task MarkReportAsDownloadedAsync(Guid reportId);
	}
}