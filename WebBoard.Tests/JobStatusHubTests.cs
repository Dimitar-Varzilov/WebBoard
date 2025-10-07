using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using WebBoard.Common.Enums;
using WebBoard.Hubs;

namespace WebBoard.Tests
{
	/// <summary>
	/// Unit tests for JobStatusHub
	/// </summary>
	public class JobStatusHubTests
	{
		private readonly Mock<ILogger<JobStatusHub>> _mockLogger;
		private readonly Mock<HubCallerContext> _mockContext;
		private readonly Mock<IGroupManager> _mockGroups;
		private readonly JobStatusHub _hub;

		public JobStatusHubTests()
		{
			_mockLogger = new Mock<ILogger<JobStatusHub>>();
			_mockContext = new Mock<HubCallerContext>();
			_mockGroups = new Mock<IGroupManager>();

			_hub = new JobStatusHub(_mockLogger.Object)
			{
				Context = _mockContext.Object,
				Groups = _mockGroups.Object
			};
		}

		#region OnConnectedAsync Tests

		[Fact]
		public async Task OnConnectedAsync_ShouldLogConnection()
		{
			// Arrange
			var connectionId = "test-connection-id";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.OnConnectedAsync();

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Client {connectionId} connected to JobStatusHub")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task OnConnectedAsync_ShouldCompleteSuccessfully()
		{
			// Arrange
			_mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");

			// Act
			Func<Task> act = _hub.OnConnectedAsync;

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region OnDisconnectedAsync Tests

		[Fact]
		public async Task OnDisconnectedAsync_ShouldLogDisconnection()
		{
			// Arrange
			var connectionId = "test-connection-id";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.OnDisconnectedAsync(null);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Client {connectionId} disconnected from JobStatusHub")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task OnDisconnectedAsync_WithException_ShouldLogDisconnection()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var exception = new Exception("Connection lost");
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.OnDisconnectedAsync(exception);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Client {connectionId} disconnected from JobStatusHub")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region SubscribeToJob Tests

		[Fact]
		public async Task SubscribeToJob_ShouldAddConnectionToGroup()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var jobId = "123e4567-e89b-12d3-a456-426614174000";
			var expectedGroupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId);

			// Assert
			_mockGroups.Verify(
				x => x.AddToGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		[Fact]
		public async Task SubscribeToJob_ShouldLogSubscription()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var jobId = "123e4567-e89b-12d3-a456-426614174000";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Client {connectionId} subscribed to job {jobId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Theory]
		[InlineData("123e4567-e89b-12d3-a456-426614174000")]
		[InlineData("987fcdeb-51a2-43f7-b890-123456789abc")]
		[InlineData("00000000-0000-0000-0000-000000000000")]
		public async Task SubscribeToJob_WithVariousJobIds_ShouldCreateCorrectGroupName(string jobId)
		{
			// Arrange
			var connectionId = "test-connection-id";
			var expectedGroupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId);

			// Assert
			_mockGroups.Verify(
				x => x.AddToGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		#endregion

		#region UnsubscribeFromJob Tests

		[Fact]
		public async Task UnsubscribeFromJob_ShouldRemoveConnectionFromGroup()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var jobId = "123e4567-e89b-12d3-a456-426614174000";
			var expectedGroupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJob(jobId);

			// Assert
			_mockGroups.Verify(
				x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJob_ShouldLogUnsubscription()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var jobId = "123e4567-e89b-12d3-a456-426614174000";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJob(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Client {connectionId} unsubscribed from job {jobId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Theory]
		[InlineData("123e4567-e89b-12d3-a456-426614174000")]
		[InlineData("987fcdeb-51a2-43f7-b890-123456789abc")]
		public async Task UnsubscribeFromJob_WithVariousJobIds_ShouldCreateCorrectGroupName(string jobId)
		{
			// Arrange
			var connectionId = "test-connection-id";
			var expectedGroupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJob(jobId);

			// Assert
			_mockGroups.Verify(
				x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task SubscribeAndUnsubscribe_ShouldWorkInSequence()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var jobId = "123e4567-e89b-12d3-a456-426614174000";
			var groupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId);
			await _hub.UnsubscribeFromJob(jobId);

			// Assert
			_mockGroups.Verify(
				x => x.AddToGroupAsync(connectionId, groupName, default),
				Times.Once);

			_mockGroups.Verify(
				x => x.RemoveFromGroupAsync(connectionId, groupName, default),
				Times.Once);
		}

		[Fact]
		public async Task MultipleSubscriptions_ShouldWorkCorrectly()
		{
			// Arrange
			var connectionId = "test-connection-id";
			var jobId1 = "123e4567-e89b-12d3-a456-426614174000";
			var jobId2 = "987fcdeb-51a2-43f7-b890-123456789abc";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId1);
			await _hub.SubscribeToJob(jobId2);

			// Assert
			_mockGroups.Verify(
				x => x.AddToGroupAsync(connectionId, $"job_{jobId1}", default),
				Times.Once);

			_mockGroups.Verify(
				x => x.AddToGroupAsync(connectionId, $"job_{jobId2}", default),
				Times.Once);
		}

		#endregion
	}

	/// <summary>
	/// Tests for JobStatusUpdateDto record
	/// </summary>
	public class JobStatusUpdateDtoTests
	{
		[Fact]
		public void JobStatusUpdateDto_ShouldCreateWithRequiredProperties()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;
			var updatedAt = DateTimeOffset.UtcNow;

			// Act
			var dto = new JobStatusUpdateDto(jobId, jobType, status, updatedAt);

			// Assert
			dto.JobId.Should().Be(jobId);
			dto.JobType.Should().Be(jobType);
			dto.Status.Should().Be(status);
			dto.UpdatedAt.Should().Be(updatedAt);
			dto.Progress.Should().BeNull();
			dto.ErrorMessage.Should().BeNull();
			dto.HasReport.Should().BeFalse();
			dto.ReportId.Should().BeNull();
			dto.ReportFileName.Should().BeNull();
			dto.TaskCount.Should().BeNull();
		}

		[Fact]
		public void JobStatusUpdateDto_ShouldCreateWithAllProperties()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Completed;
			var updatedAt = DateTimeOffset.UtcNow;
			var progress = 100;
			var errorMessage = "Test error";
			var reportId = Guid.NewGuid();
			var reportFileName = "report.txt";
			var taskCount = 5;

			// Act
			var dto = new JobStatusUpdateDto(
				jobId,
				jobType,
				status,
				updatedAt,
				progress,
				errorMessage,
				true,
				reportId,
				reportFileName,
				taskCount);

			// Assert
			dto.JobId.Should().Be(jobId);
			dto.JobType.Should().Be(jobType);
			dto.Status.Should().Be(status);
			dto.UpdatedAt.Should().Be(updatedAt);
			dto.Progress.Should().Be(progress);
			dto.ErrorMessage.Should().Be(errorMessage);
			dto.HasReport.Should().BeTrue();
			dto.ReportId.Should().Be(reportId);
			dto.ReportFileName.Should().Be(reportFileName);
			dto.TaskCount.Should().Be(taskCount);
		}

		[Theory]
		[InlineData(JobStatus.Queued)]
		[InlineData(JobStatus.Running)]
		[InlineData(JobStatus.Completed)]
		[InlineData(JobStatus.Failed)]
		public void JobStatusUpdateDto_ShouldSupportAllJobStatuses(JobStatus status)
		{
			// Arrange & Act
			var dto = new JobStatusUpdateDto(
				Guid.NewGuid(),
				"TestJob",
				status,
				DateTimeOffset.UtcNow);

			// Assert
			dto.Status.Should().Be(status);
		}

		[Fact]
		public void JobStatusUpdateDto_WithDeconstruction_ShouldWorkCorrectly()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;
			var updatedAt = DateTimeOffset.UtcNow;

			var dto = new JobStatusUpdateDto(jobId, jobType, status, updatedAt);

			// Act
			var (id, type, stat, updated, _, _, _, _, _, _) = dto;

			// Assert
			id.Should().Be(jobId);
			type.Should().Be(jobType);
			stat.Should().Be(status);
			updated.Should().Be(updatedAt);
		}

		[Fact]
		public void JobStatusUpdateDto_Equality_ShouldWorkCorrectly()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = "TestJob";
			var status = JobStatus.Running;
			var updatedAt = DateTimeOffset.UtcNow;

			var dto1 = new JobStatusUpdateDto(jobId, jobType, status, updatedAt);
			var dto2 = new JobStatusUpdateDto(jobId, jobType, status, updatedAt);
			var dto3 = new JobStatusUpdateDto(Guid.NewGuid(), jobType, status, updatedAt);

			// Act & Assert
			dto1.Should().Be(dto2); // Same values
			dto1.Should().NotBe(dto3); // Different JobId
		}
	}
}
