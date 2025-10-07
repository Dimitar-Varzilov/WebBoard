using Microsoft.AspNetCore.Mvc;
using WebBoard.Common.Constants;
using WebBoard.Services.Reports;

namespace WebBoard.Controllers
{
	[ApiController]
	[Route("api/reports")]
	[Tags(Constants.SwaggerTags.Reports)]
	public class ReportsController(IReportService reportService) : ControllerBase
	{
		/// <summary>
		/// Download a report by its ID
		/// </summary>
		/// <param name="id">The report ID</param>
		/// <returns>The report file for download</returns>
		[HttpGet("{id:guid}/download")]
		[ProducesResponseType(typeof(FileContentResult), 200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> DownloadReport(Guid id)
		{
			var report = await reportService.GetReportForDownloadAsync(id);
			if (report == null)
			{
				return NotFound(new { message = "Report not found" });
			}

			// Mark report as downloaded
			await reportService.MarkReportAsDownloadedAsync(id);

			// Convert content to bytes
			var contentBytes = System.Text.Encoding.UTF8.GetBytes(report.Content);

			return File(
				contentBytes,
				report.ContentType,
				report.FileName
			);
		}

		/// <summary>
		/// Get report information by job ID
		/// </summary>
		/// <param name="jobId">The job ID</param>
		/// <returns>Report information</returns>
		[HttpGet("by-job/{jobId:guid}")]
		[ProducesResponseType(200)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> GetReportByJobId(Guid jobId)
		{
			var report = await reportService.GetReportByJobIdAsync(jobId);
			return report == null ? NotFound(new { message = "Report not found for this job" }) : Ok(report);
		}
	}
}