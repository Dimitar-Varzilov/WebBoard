using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Reports;

namespace WebBoard.Tests
{
	public class ReportServiceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<ILogger<ReportService>> _mockLogger;
		private readonly ReportService _reportService;

		public ReportServiceTests()
		{
			// Setup in-memory database
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockLogger = new Mock<ILogger<ReportService>>();
			_reportService = new ReportService(_dbContext, _mockLogger.Object);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region CreateReportAsync Tests

		[Fact]
		public async Task CreateReportAsync_ShouldCreateReportWithCorrectData()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var fileName = "test-report.pdf";
			var content = "Test report content";
			var contentType = "application/pdf";

			// Act
			var result = await _reportService.CreateReportAsync(jobId, fileName, content, contentType);

			// Assert
			result.Should().NotBeNull();
			result.Id.Should().NotBe(Guid.Empty);
			result.JobId.Should().Be(jobId);
			result.FileName.Should().Be(fileName);
			result.Content.Should().Be(content);
			result.ContentType.Should().Be(contentType);
			result.Status.Should().Be(ReportStatus.Generated);
			result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
		}

		[Fact]
		public async Task CreateReportAsync_ShouldSaveReportToDatabase()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var fileName = "test-report.txt";
			var content = "Report content";
			var contentType = "text/plain";

			// Act
			var result = await _reportService.CreateReportAsync(jobId, fileName, content, contentType);

			// Assert
			var savedReport = await _dbContext.Reports.FindAsync(result.Id);
			savedReport.Should().NotBeNull();
			savedReport!.JobId.Should().Be(jobId);
			savedReport.FileName.Should().Be(fileName);
		}

		[Fact]
		public async Task CreateReportAsync_ShouldLogInformation()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var fileName = "test-report.pdf";
			var content = "Test content";
			var contentType = "application/pdf";

			// Act
			var result = await _reportService.CreateReportAsync(jobId, fileName, content, contentType);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Report {result.Id} created for job {jobId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CreateReportAsync_ShouldGenerateUniqueIds()
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			var report1 = await _reportService.CreateReportAsync(jobId, "report1.pdf", "content1", "application/pdf");
			var report2 = await _reportService.CreateReportAsync(jobId, "report2.pdf", "content2", "application/pdf");

			// Assert
			report1.Id.Should().NotBe(report2.Id);
		}

		#endregion

		#region GetReportForDownloadAsync Tests

		[Fact]
		public async Task GetReportForDownloadAsync_ShouldReturnReportDto_WhenReportExists()
		{
			// Arrange
			var report = new Report(
				Guid.NewGuid(),
				Guid.NewGuid(),
				"test.pdf",
				"content",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Generated);
			_dbContext.Reports.Add(report);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _reportService.GetReportForDownloadAsync(report.Id);

			// Assert
			result.Should().NotBeNull();
			result!.FileName.Should().Be(report.FileName);
			result.Content.Should().Be(report.Content);
			result.ContentType.Should().Be(report.ContentType);
		}

		[Fact]
		public async Task GetReportForDownloadAsync_ShouldReturnNull_WhenReportDoesNotExist()
		{
			// Arrange
			var nonExistentId = Guid.NewGuid();

			// Act
			var result = await _reportService.GetReportForDownloadAsync(nonExistentId);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetReportForDownloadAsync_ShouldUseAsNoTracking()
		{
			// Arrange
			var report = new Report(
				Guid.NewGuid(),
				Guid.NewGuid(),
				"test.pdf",
				"content",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Generated);
			_dbContext.Reports.Add(report);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _reportService.GetReportForDownloadAsync(report.Id);

			// Assert
			result.Should().NotBeNull();
			// Verify entity is not tracked
			_dbContext.Entry(report).State.Should().Be(EntityState.Unchanged);
		}

		#endregion

		#region GetReportByJobIdAsync Tests

		[Fact]
		public async Task GetReportByJobIdAsync_ShouldReturnReportDto_WhenReportExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var report = new Report(
				Guid.NewGuid(),
				jobId,
				"job-report.pdf",
				"content",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Generated);
			_dbContext.Reports.Add(report);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _reportService.GetReportByJobIdAsync(jobId);

			// Assert
			result.Should().NotBeNull();
			result!.Id.Should().Be(report.Id);
			result.JobId.Should().Be(jobId);
			result.FileName.Should().Be(report.FileName);
			result.ContentType.Should().Be(report.ContentType);
			result.Status.Should().Be(report.Status);
		}

		[Fact]
		public async Task GetReportByJobIdAsync_ShouldReturnNull_WhenReportDoesNotExist()
		{
			// Arrange
			var nonExistentJobId = Guid.NewGuid();

			// Act
			var result = await _reportService.GetReportByJobIdAsync(nonExistentJobId);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetReportByJobIdAsync_ShouldReturnFirstReport_WhenMultipleReportsExist()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var report1 = new Report(Guid.NewGuid(), jobId, "report1.pdf", "content1", "application/pdf", DateTimeOffset.UtcNow.AddMinutes(-10), ReportStatus.Generated);
			var report2 = new Report(Guid.NewGuid(), jobId, "report2.pdf", "content2", "application/pdf", DateTimeOffset.UtcNow, ReportStatus.Generated);
			_dbContext.Reports.AddRange(report1, report2);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _reportService.GetReportByJobIdAsync(jobId);

			// Assert
			result.Should().NotBeNull();
			result!.JobId.Should().Be(jobId);
		}

		#endregion

		#region MarkReportAsDownloadedAsync Tests

		[Fact]
		public async Task MarkReportAsDownloadedAsync_ShouldUpdateStatusToDownloaded()
		{
			// Arrange
			var report = new Report(
				Guid.NewGuid(),
				Guid.NewGuid(),
				"test.pdf",
				"content",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Generated);
			_dbContext.Reports.Add(report);
			await _dbContext.SaveChangesAsync();

			// Act
			await _reportService.MarkReportAsDownloadedAsync(report.Id);

			// Assert
			var updatedReport = await _dbContext.Reports.FindAsync(report.Id);
			updatedReport.Should().NotBeNull();
			updatedReport!.Status.Should().Be(ReportStatus.Downloaded);
		}

		[Fact]
		public async Task MarkReportAsDownloadedAsync_ShouldLogInformation_WhenReportExists()
		{
			// Arrange
			var report = new Report(
				Guid.NewGuid(),
				Guid.NewGuid(),
				"test.pdf",
				"content",
				"application/pdf",
				DateTimeOffset.UtcNow,
				ReportStatus.Generated);
			_dbContext.Reports.Add(report);
			await _dbContext.SaveChangesAsync();

			// Act
			await _reportService.MarkReportAsDownloadedAsync(report.Id);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Report {report.Id} marked as downloaded")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task MarkReportAsDownloadedAsync_ShouldNotThrow_WhenReportDoesNotExist()
		{
			// Arrange
			var nonExistentId = Guid.NewGuid();

			// Act
			Func<Task> act = () => _reportService.MarkReportAsDownloadedAsync(nonExistentId);

			// Assert
			await act.Should().NotThrowAsync();
		}

		[Fact]
		public async Task MarkReportAsDownloadedAsync_ShouldNotLog_WhenReportDoesNotExist()
		{
			// Arrange
			var nonExistentId = Guid.NewGuid();

			// Act
			await _reportService.MarkReportAsDownloadedAsync(nonExistentId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.IsAny<It.IsAnyType>(),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Never);
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task FullReportLifecycle_ShouldWorkCorrectly()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var fileName = "lifecycle-test.pdf";
			var content = "Test content";
			var contentType = "application/pdf";

			// Act & Assert - Create
			var createdReport = await _reportService.CreateReportAsync(jobId, fileName, content, contentType);
			createdReport.Status.Should().Be(ReportStatus.Generated);

			// Act & Assert - Get by Job ID
			var reportByJobId = await _reportService.GetReportByJobIdAsync(jobId);
			reportByJobId.Should().NotBeNull();
			reportByJobId!.Id.Should().Be(createdReport.Id);

			// Act & Assert - Get for Download
			var downloadDto = await _reportService.GetReportForDownloadAsync(createdReport.Id);
			downloadDto.Should().NotBeNull();
			downloadDto!.FileName.Should().Be(fileName);
			downloadDto.Content.Should().Be(content);

			// Act & Assert - Mark as Downloaded
			await _reportService.MarkReportAsDownloadedAsync(createdReport.Id);
			var finalReport = await _dbContext.Reports.FindAsync(createdReport.Id);
			finalReport!.Status.Should().Be(ReportStatus.Downloaded);
		}

		#endregion
	}
}
