using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using WebBoard.API.Common.Constants;
using WebBoard.API.Hubs;

namespace WebBoard.Tests.Hubs
{
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

		#region Connection Tests

		[Fact]
		public async Task OnConnectedAsync_ShouldLogConnectionInfo()
		{
			// Arrange
			var connectionId = "test-connection-123";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.OnConnectedAsync();

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId) && v.ToString()!.Contains("connected")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task OnDisconnectedAsync_ShouldLogDisconnectionInfo()
		{
			// Arrange
			var connectionId = "test-connection-456";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.OnDisconnectedAsync(null);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId) && v.ToString()!.Contains("disconnected")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task OnDisconnectedAsync_WithException_ShouldStillLogDisconnection()
		{
			// Arrange
			var connectionId = "test-connection-789";
			var exception = new Exception("Connection lost");
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.OnDisconnectedAsync(exception);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId)),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region Subscribe/Unsubscribe Single Job Tests

		[Fact]
		public async Task SubscribeToJob_ShouldAddClientToJobGroup()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-1";
			var expectedGroupName = SignalRConstants.Groups.GetJobGroup(jobId);

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId.ToString());

			// Assert
			_mockGroups.Verify(
				g => g.AddToGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		[Fact]
		public async Task SubscribeToJob_ShouldLogSubscription()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-2";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId.ToString());

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId) && v.ToString()!.Contains("subscribed")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJob_ShouldRemoveClientFromJobGroup()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-3";
			var expectedGroupName = SignalRConstants.Groups.GetJobGroup(jobId);

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJob(jobId.ToString());

			// Assert
			_mockGroups.Verify(
				g => g.RemoveFromGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJob_ShouldLogUnsubscription()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-4";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJob(jobId.ToString());

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId) && v.ToString()!.Contains("unsubscribed")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region Batch Subscribe/Unsubscribe Tests

		[Fact]
		public async Task SubscribeToJobs_WithMultipleJobs_ShouldAddToAllGroups()
		{
			// Arrange
			var jobIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
			var connectionId = "connection-5";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJobs(jobIds);

			// Assert
			_mockGroups.Verify(
				g => g.AddToGroupAsync(connectionId, It.IsAny<string>(), default),
				Times.Exactly(jobIds.Length));
		}

		[Fact]
		public async Task SubscribeToJobs_WithNullArray_ShouldLogWarningAndNotSubscribe()
		{
			// Arrange
			var connectionId = "connection-6";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJobs(null!);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty job list")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);

			_mockGroups.Verify(
				g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
				Times.Never);
		}

		[Fact]
		public async Task SubscribeToJobs_WithEmptyArray_ShouldLogWarningAndNotSubscribe()
		{
			// Arrange
			var connectionId = "connection-7";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJobs([]);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty job list")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task SubscribeToJobs_WithInvalidJobId_ShouldThrowException()
		{
			// Arrange
			var jobIds = new[] { "invalid-guid" };
			var connectionId = "connection-8";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act & Assert
			// The Hub will throw because Guid.Parse fails in SubscribeToJob
			// and Task.WhenAll will propagate the exception
			await Assert.ThrowsAsync<FormatException>(() => _hub.SubscribeToJobs(jobIds));
		}

		[Fact]
		public async Task SubscribeToJobs_ShouldLogBatchSubscriptionCount()
		{
			// Arrange
			var jobIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
			var connectionId = "connection-9";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJobs(jobIds);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("subscribed to") && v.ToString()!.Contains("jobs")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJobs_WithMultipleJobs_ShouldRemoveFromAllGroups()
		{
			// Arrange
			var jobIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
			var connectionId = "connection-10";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJobs(jobIds);

			// Assert
			_mockGroups.Verify(
				g => g.RemoveFromGroupAsync(connectionId, It.IsAny<string>(), default),
				Times.Exactly(jobIds.Length));
		}

		[Fact]
		public async Task UnsubscribeFromJobs_WithNullArray_ShouldLogWarningAndNotUnsubscribe()
		{
			// Arrange
			var connectionId = "connection-11";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJobs(null!);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty job list")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);

			_mockGroups.Verify(
				g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default),
				Times.Never);
		}

		[Fact]
		public async Task UnsubscribeFromJobs_WithEmptyArray_ShouldLogWarningAndNotUnsubscribe()
		{
			// Arrange
			var connectionId = "connection-12";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJobs([]);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty job list")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJobs_WithInvalidJobId_ShouldLogError()
		{
			// Arrange
			var jobIds = new[] { "not-a-guid" };
			var connectionId = "connection-13";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJobs(jobIds);

			// Assert
			// The exception is caught and logged
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error unsubscribing")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJobs_ShouldLogBatchUnsubscriptionCount()
		{
			// Arrange
			var jobIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
			var connectionId = "connection-14";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJobs(jobIds);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unsubscribed from") && v.ToString()!.Contains("jobs")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region Group Name Tests

		[Fact]
		public async Task SubscribeToJob_ShouldUseCorrectGroupNameFormat()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-15";
			var expectedGroupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJob(jobId.ToString());

			// Assert
			_mockGroups.Verify(
				g => g.AddToGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		[Fact]
		public async Task UnsubscribeFromJob_ShouldUseCorrectGroupNameFormat()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-16";
			var expectedGroupName = $"job_{jobId}";

			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.UnsubscribeFromJob(jobId.ToString());

			// Assert
			_mockGroups.Verify(
				g => g.RemoveFromGroupAsync(connectionId, expectedGroupName, default),
				Times.Once);
		}

		#endregion

		#region Edge Cases

		[Fact]
		public async Task SubscribeToJob_WithDifferentCaseGuid_ShouldHandleCorrectly()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var connectionId = "connection-17";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act - using uppercase GUID
			await _hub.SubscribeToJob(jobId.ToString().ToUpper());

			// Assert - should still work (GUID parsing is case-insensitive)
			_mockGroups.Verify(
				g => g.AddToGroupAsync(connectionId, It.IsAny<string>(), default),
				Times.Once);
		}

		[Fact]
		public async Task SubscribeToJobs_WithSingleJob_ShouldStillWorkAsBatch()
		{
			// Arrange
			var jobIds = new[] { Guid.NewGuid().ToString() };
			var connectionId = "connection-18";
			_mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

			// Act
			await _hub.SubscribeToJobs(jobIds);

			// Assert
			_mockGroups.Verify(
				g => g.AddToGroupAsync(connectionId, It.IsAny<string>(), default),
				Times.Once);
		}

		#endregion
	}
}
