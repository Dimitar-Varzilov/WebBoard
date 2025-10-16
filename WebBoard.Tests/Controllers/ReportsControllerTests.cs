using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebBoard.API.Common.DTOs.Reports;
using WebBoard.API.Common.Enums;
using WebBoard.API.Controllers;
using WebBoard.API.Services.Reports;

namespace WebBoard.Tests.Controllers
{
	public class ReportsControllerTests
	{
		private readonly Mock<IReportService> _mockReportService;
		private readonly ReportsController _controller;

		public ReportsControllerTests()
		{
			_mockReportService = new Mock<IReportService>();
			_controller = new ReportsController(_mockReportService.Object);
		}

		#region DownloadReport Tests

		[Fact]
		public async Task DownloadReport_WhenReportExists_ShouldReturnFileResult()
		{
			// Arrange
			var reportId = Guid.NewGuid();
			var report = new ReportDownloadDto(
				"test-report.pdf",
				"Sample PDF content",
				"application/pdf");

			_mockReportService.Setup(s => s.GetReportForDownloadAsync(reportId))
				.ReturnsAsync(report);

			_mockReportService.Setup(s => s.MarkReportAsDownloadedAsync(reportId))
				.Returns(Task.CompletedTask);

			// Act
			var result = await _controller.DownloadReport(reportId);

			// Assert
			var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
			fileResult.FileDownloadName.Should().Be(report.FileName);
			fileResult.ContentType.Should().Be(report.ContentType);
			fileResult.FileContents.Should().NotBeEmpty();

			_mockReportService.Verify(s => s.MarkReportAsDownloadedAsync(reportId), Times.Once);
		}

		[Fact]
		public async Task DownloadReport_WhenReportDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var reportId = Guid.NewGuid();
			_mockReportService.Setup(s => s.GetReportForDownloadAsync(reportId))
				.ReturnsAsync((ReportDownloadDto?)null);

			// Act
			var result = await _controller.DownloadReport(reportId);

			// Assert
			var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
			notFoundResult.Value.Should().NotBeNull();

			_mockReportService.Verify(s => s.MarkReportAsDownloadedAsync(It.IsAny<Guid>()), Times.Never);
		}

		[Fact]
		public async Task DownloadReport_ShouldConvertContentToBytesCorrectly()
		{
			// Arrange
			var reportId = Guid.NewGuid();
			var content = "Test report content with special chars: ?ˆ?";
			var report = new ReportDownloadDto(
				"test.txt",
				content,
				"text/plain");

			_mockReportService.Setup(s => s.GetReportForDownloadAsync(reportId))
				.ReturnsAsync(report);

			// Act
			var result = await _controller.DownloadReport(reportId);

			// Assert
			var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
			var actualContent = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);
			actualContent.Should().Be(content);
		}

		[Fact]
		public async Task DownloadReport_WithDifferentContentTypes_ShouldSetCorrectMimeType()
		{
			// Arrange
			var reportId = Guid.NewGuid();
			var testCases = new[]
			{
				("application/pdf", "report.pdf"),
				("text/plain", "report.txt"),
				("text/csv", "report.csv"),
				("application/json", "report.json")
			};

			foreach (var (contentType, fileName) in testCases)
			{
				var report = new ReportDownloadDto(fileName, "content", contentType);
				_mockReportService.Setup(s => s.GetReportForDownloadAsync(reportId))
					.ReturnsAsync(report);

				// Act
				var result = await _controller.DownloadReport(reportId);

				// Assert
				var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
				fileResult.ContentType.Should().Be(contentType);
				fileResult.FileDownloadName.Should().Be(fileName);
			}
		}

		#endregion

		#region GetReportByJobId Tests

		[Fact]
		public async Task GetReportByJobId_WhenReportExists_ShouldReturnOk()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var report = new ReportDto(
				Guid.NewGuid(),
				jobId,
				"job-report.pdf",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Generated);

			_mockReportService.Setup(s => s.GetReportByJobIdAsync(jobId))
				.ReturnsAsync(report);

			// Act
			var result = await _controller.GetReportByJobId(jobId);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(report);
		}

		[Fact]
		public async Task GetReportByJobId_WhenReportDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockReportService.Setup(s => s.GetReportByJobIdAsync(jobId))
				.ReturnsAsync((ReportDto?)null);

			// Act
			var result = await _controller.GetReportByJobId(jobId);

			// Assert
			var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
			notFoundResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task GetReportByJobId_WithDownloadedReport_ShouldReturnCorrectStatus()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var report = new ReportDto(
				Guid.NewGuid(),
				jobId,
				"downloaded-report.pdf",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Downloaded);

			_mockReportService.Setup(s => s.GetReportByJobIdAsync(jobId))
				.ReturnsAsync(report);

			// Act
			var result = await _controller.GetReportByJobId(jobId);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var returnedReport = okResult.Value.Should().BeAssignableTo<ReportDto>().Subject;
			returnedReport.Status.Should().Be(ReportStatus.Downloaded);
		}

		#endregion
	}
}
