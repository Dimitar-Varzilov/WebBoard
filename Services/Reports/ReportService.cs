using Microsoft.EntityFrameworkCore;
using WebBoard.Common.DTOs.Reports;
using WebBoard.Common.Enums;
using WebBoard.Common.Models;
using WebBoard.Data;

namespace WebBoard.Services.Reports
{
	public class ReportService(AppDbContext db, ILogger<ReportService> logger): IReportService
	{
		public async Task<Report> CreateReportAsync(Guid jobId, string fileName, string content, string contentType)
		{
			var report = new Report(
				Guid.NewGuid(),
				jobId,
				fileName,
				content,
				contentType,
				DateTimeOffset.UtcNow,
				ReportStatus.Generated
			);

			db.Reports.Add(report);
			await db.SaveChangesAsync();

			logger.LogInformation("Report {ReportId} created for job {JobId}", report.Id, jobId);
			return report;
		}

		public async Task<ReportDownloadDto?> GetReportForDownloadAsync(Guid reportId)
		{
			var report = await db.Reports
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.Id == reportId);

			if (report == null)
			{
				return null;
			}

			return new ReportDownloadDto(report.FileName, report.Content, report.ContentType);
		}

		public async Task<ReportDto?> GetReportByJobIdAsync(Guid jobId)
		{
			var report = await db.Reports
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.JobId == jobId);

			if (report == null)
			{
				return null;
			}

			return new ReportDto(report.Id, report.JobId, report.FileName, report.ContentType, report.CreatedAt, report.Status);
		}

		public async Task MarkReportAsDownloadedAsync(Guid reportId)
		{
			var report = await db.Reports.FindAsync(reportId);
			if (report != null)
			{
				var updatedReport = report with { Status = ReportStatus.Downloaded };
				db.Entry(report).CurrentValues.SetValues(updatedReport);
				await db.SaveChangesAsync();

				logger.LogInformation("Report {ReportId} marked as downloaded", reportId);
			}
		}
	}
}