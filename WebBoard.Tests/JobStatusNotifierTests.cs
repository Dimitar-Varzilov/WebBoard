using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Jobs;
using WebBoard.API.Common.Enums;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Hubs;

namespace WebBoard.Tests
{
	/// <summary>
	/// Unit tests for JobStatusNotifier
	/// </summary>
	public class JobStatusNotifierTests
	{
		private readonly Mock<IHubContext<JobStatusHub>> _mockHubContext;
		private readonly Mock<ILogger<JobStatusNotifier>> _mockLogger;
		private readonly Mock<IHubClients> _mockClients;
		private readonly Mock<IClientProxy> _mockAllClientsProxy;
		private readonly Mock<IClientProxy> _mockGroupClientsProxy;
		private readonly JobStatusNotifier _notifier;

		public JobStatusNotifierTests()
		{
			_mockHubContext = new Mock<IHubContext<JobStatusHub>>();
			_mockLogger = new Mock<ILogger<JobStatusNotifier>>();
			_mockClients = new Mock<IHubClients>();
			_mockAllClientsProxy = new Mock<IClientProxy>();
			_mockGroupClientsProxy = new Mock<IClientProxy>();

			_mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
			_mockClients.Setup(c => c.All).Returns(_mockAllClientsProxy.Object);
			_mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockGroupClientsProxy.Object);

			_notifier = new JobStatusNotifier(_mockHubContext.Object, _mockLogger.Object);
		}

		#region NotifyJobStatusAsync Tests

		[Fact]
		public async Task NotifyJobStatusAsync_ShouldBroadcastToAllClients()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					SignalRConstants.Methods.JobStatusUpdated,
					It.Is<object[]>(o => o.Length == 1 && ((JobStatusUpdateDto)o[0]).JobId == jobId),
					default),
				Times.Once);
		}
		[Fact]
		public async Task NotifyJobStatusAsync_ShouldBroadcastToSpecificJobGroup()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Completed;
			var expectedGroupName = $"job_{jobId}";

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockClients.Verify(
			x => x.Group(expectedGroupName),
			Times.Once);

			_mockGroupClientsProxy.Verify(
				x => x.SendCoreAsync(
					SignalRConstants.Methods.JobStatusUpdated,
					It.Is<object[]>(o => o.Length == 1 && ((JobStatusUpdateDto)o[0]).JobId == jobId),
					default),
				Times.Once);
		}
		[Fact]
		public async Task NotifyJobStatusAsync_ShouldIncludeJobTypeInUpdate()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "DataProcessingJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					SignalRConstants.Methods.JobStatusUpdated,
					It.Is<object[]>(o => o.Length == 1 && ((JobStatusUpdateDto)o[0]).JobType == jobType),
					default),
				Times.Once);
		}
		[Fact]
		public async Task NotifyJobStatusAsync_ShouldIncludeStatusInUpdate()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Failed;

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o => o.Length == 1 && ((JobStatusUpdateDto)o[0]).Status == status),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobStatusAsync_WithErrorMessage_ShouldIncludeErrorInUpdate()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Failed;
			var errorMessage = "Test error occurred";

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status, errorMessage);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).ErrorMessage == errorMessage),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobStatusAsync_WithoutErrorMessage_ShouldHaveNullError()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).ErrorMessage == null),
					default),
				Times.Once);
		}

		[Theory]
		[InlineData(JobStatus.Queued)]
		[InlineData(JobStatus.Running)]
		[InlineData(JobStatus.Completed)]
		[InlineData(JobStatus.Failed)]
		public async Task NotifyJobStatusAsync_WithDifferentStatuses_ShouldBroadcastCorrectly(JobStatus status)
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).Status == status),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobStatusAsync_ShouldLogInformation()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Broadcasted job status update")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobStatusAsync_WhenExceptionThrown_ShouldLogError()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;
			var expectedException = new Exception("SignalR error");

			_mockAllClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(expectedException);

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to broadcast job status update for job {jobId}")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobStatusAsync_WhenExceptionThrown_ShouldNotThrow()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			_mockAllClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(new Exception("SignalR error"));

			// Act
			Func<Task> act = () => _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region NotifyJobProgressAsync Tests

		[Fact]
		public async Task NotifyJobProgressAsync_ShouldBroadcastToAllClients()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var progress = 50;

			// Act
			await _notifier.NotifyJobProgressAsync(jobId, progress);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					SignalRConstants.Methods.JobProgressUpdated,
					It.Is<object[]>(o => o.Length == 1 && ((JobProgressUpdateDto)o[0]).JobId == jobId),
					default),
				Times.Once);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(25)]
		[InlineData(50)]
		[InlineData(75)]
		[InlineData(100)]
		public async Task NotifyJobProgressAsync_WithDifferentProgressValues_ShouldBroadcast(int progress)
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			await _notifier.NotifyJobProgressAsync(jobId, progress);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					SignalRConstants.Methods.JobProgressUpdated,
					It.IsAny<object[]>(),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobProgressAsync_ShouldLogDebug()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var progress = 75;

			// Act
			await _notifier.NotifyJobProgressAsync(jobId, progress);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Broadcasted job progress") &&
						v.ToString()!.Contains($"{progress}%")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobProgressAsync_WhenExceptionThrown_ShouldLogError()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var progress = 50;
			var expectedException = new Exception("SignalR error");

			_mockAllClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(expectedException);

			// Act
			await _notifier.NotifyJobProgressAsync(jobId, progress);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to broadcast job progress for job {jobId}")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobProgressAsync_WhenExceptionThrown_ShouldNotThrow()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var progress = 50;

			_mockAllClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(new Exception("SignalR error"));

			// Act
			Func<Task> act = () => _notifier.NotifyJobProgressAsync(jobId, progress);

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region NotifyReportGeneratedAsync Tests

		[Fact]
		public async Task NotifyReportGeneratedAsync_ShouldBroadcastToAllClients()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"ReportGenerated",
					It.Is<object[]>(o => o.Length == 1),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_ShouldIncludeReportId()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"ReportGenerated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).ReportId == reportId),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_ShouldIncludeFileName()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "monthly-report.xlsx";

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"ReportGenerated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).ReportFileName == fileName),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_ShouldSetHasReportToTrue()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"ReportGenerated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).HasReport == true),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_ShouldSetStatusToCompleted()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"ReportGenerated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).Status == JobStatus.Completed),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_ShouldLogInformation()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Broadcasted report generation") &&
						v.ToString()!.Contains($"{jobId}") &&
						v.ToString()!.Contains($"{reportId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_WhenExceptionThrown_ShouldLogError()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";
			var expectedException = new Exception("SignalR error");

			_mockAllClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(expectedException);

			// Act
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to broadcast report generation for job {jobId}")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyReportGeneratedAsync_WhenExceptionThrown_ShouldNotThrow()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			_mockAllClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(new Exception("SignalR error"));

			// Act
			Func<Task> act = () => _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region NotifyJobGroupAsync Tests

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldSendToSpecificGroup()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;
			var expectedGroupName = $"job_{jobId}";

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockClients.Verify(
				x => x.Group(expectedGroupName),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldNotSendToAllClients()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockClients.Verify(
				x => x.All,
				Times.Never);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldSendJobStatusUpdatedMessage()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Completed;

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockGroupClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o => o.Length == 1),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldIncludeCorrectJobId()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockGroupClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).JobId == jobId),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldIncludeCorrectJobType()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "DataProcessingJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockGroupClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).JobType == jobType),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldIncludeCorrectStatus()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Failed;

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockGroupClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.Is<object[]>(o =>
						o.Length == 1 &&
						((JobStatusUpdateDto)o[0]).Status == status),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_ShouldLogDebug()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Notified job group for job {jobId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_WhenExceptionThrown_ShouldLogError()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;
			var expectedException = new Exception("SignalR error");

			_mockGroupClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(expectedException);

			// Act
			await _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to notify job group for job {jobId}")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task NotifyJobGroupAsync_WhenExceptionThrown_ShouldNotThrow()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			_mockGroupClientsProxy
				.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
				.ThrowsAsync(new Exception("SignalR error"));

			// Act
			Func<Task> act = () => _notifier.NotifyJobGroupAsync(jobId, jobType, status);

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task MultipleNotifications_ShouldWorkInSequence()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, JobStatus.Queued);
			await _notifier.NotifyJobStatusAsync(jobId, jobType, JobStatus.Running);
			await _notifier.NotifyJobProgressAsync(jobId, 50);
			await _notifier.NotifyJobStatusAsync(jobId, jobType, JobStatus.Completed);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.IsAny<object[]>(),
					default),
			Times.Exactly(3));

			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					SignalRConstants.Methods.JobProgressUpdated,
					It.IsAny<object[]>(),
					default),
				Times.Once);
		}
		[Fact]
		public async Task NotifyJobWithReport_ShouldSendBothStatusAndReportNotifications()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "ReportJob";
			var reportId = Guid.NewGuid();
			var fileName = "report.pdf";

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, JobStatus.Running);
			await _notifier.NotifyReportGeneratedAsync(jobId, reportId, fileName);

			// Assert
			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"JobStatusUpdated",
					It.IsAny<object[]>(),
					default),
				Times.Once);

			_mockAllClientsProxy.Verify(
				x => x.SendCoreAsync(
					"ReportGenerated",
					It.IsAny<object[]>(),
					default),
				Times.Once);
		}

		[Fact]
		public async Task NotifyBothAllClientsAndGroup_ShouldSendToBoth()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;

			// Act
			await _notifier.NotifyJobStatusAsync(jobId, jobType, status);

			// Assert - Should send to both All and Group
			_mockClients.Verify(x => x.All, Times.AtLeastOnce);
			_mockClients.Verify(x => x.Group($"job_{jobId}"), Times.AtLeastOnce);
		}

		#endregion
	}
}
