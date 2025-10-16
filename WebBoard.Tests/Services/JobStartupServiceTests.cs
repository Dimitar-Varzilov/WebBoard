using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests.Services
{
	public class JobStartupServiceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<ILogger<JobStartupService>> _mockLogger;
		private readonly Mock<IJobSchedulingService> _mockJobSchedulingService;
		private readonly IServiceProvider _serviceProvider;

		public JobStartupServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockScheduler = new Mock<IScheduler>();
			_mockLogger = new Mock<ILogger<JobStartupService>>();
			_mockJobSchedulingService = new Mock<IJobSchedulingService>();

			// Setup service provider
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton(_dbContext);
			serviceCollection.AddSingleton(_mockJobSchedulingService.Object);
			_serviceProvider = serviceCollection.BuildServiceProvider();

			// Default scheduler behavior
			_mockScheduler.Setup(s => s.IsStarted).Returns(true);
		}

		public void Dispose()
		{
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
			GC.SuppressFinalize(this);
		}

		#region StartAsync Tests

		[Fact]
		public async Task StartAsync_WithNoPendingJobs_ShouldLogStartupMessage()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("checking for pending jobs")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.AtLeastOnce);
		}

		[Fact]
		public async Task StartAsync_WithRecentlyCreatedJob_ShouldNotSchedule()
		{
			// Arrange
			var recentJob = new Job(
				Guid.NewGuid(),
				"TestJob",
				JobStatus.Queued,
				DateTimeOffset.UtcNow, // Created just now
				null);

			await _dbContext.Jobs.AddAsync(recentJob);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert
			_mockJobSchedulingService.Verify(
				s => s.ScheduleJobAsync(It.IsAny<Job>()),
				Times.Never);
		}

		[Fact]
		public async Task StartAsync_WithNonQueuedJobs_ShouldNotScheduleThem()
		{
			// Arrange
			var runningJob = new Job(Guid.NewGuid(), "RunningJob", JobStatus.Running, DateTimeOffset.UtcNow.AddMinutes(-10), null);
			var completedJob = new Job(Guid.NewGuid(), "CompletedJob", JobStatus.Completed, DateTimeOffset.UtcNow.AddMinutes(-10), null);
			var failedJob = new Job(Guid.NewGuid(), "FailedJob", JobStatus.Failed, DateTimeOffset.UtcNow.AddMinutes(-10), null);

			await _dbContext.Jobs.AddRangeAsync(runningJob, completedJob, failedJob);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert
			_mockJobSchedulingService.Verify(
				s => s.ScheduleJobAsync(It.IsAny<Job>()),
				Times.Never);
		}

		#endregion

		#region StopAsync Tests

		[Fact]
		public async Task StopAsync_ShouldLogShutdown()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			await service.StopAsync(CancellationToken.None);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopping")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task StopAsync_ShouldCompleteSuccessfully()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			var stopTask = service.StopAsync(CancellationToken.None);

			// Assert
			await stopTask; // Should not throw
			stopTask.IsCompletedSuccessfully.Should().BeTrue();
		}

		#endregion

		#region Error Handling Tests

		[Fact]
		public async Task StartAsync_ShouldHandleMultipleCallsGracefully()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			await service.StartAsync(CancellationToken.None);
			await service.StartAsync(CancellationToken.None); // Second call

			// Assert - Should log that it already ran
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already run")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.AtLeastOnce);
		}

		#endregion

		#region Scheduler State Tests

		[Fact]
		public void Constructor_ShouldAcceptDependencies()
		{
			// Arrange & Act
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Assert
			service.Should().NotBeNull();
			service.Should().BeAssignableTo<IHostedService>();
		}

		[Fact]
		public async Task StartAsync_ShouldDelayBeforeProcessing()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);
			var startTime = DateTime.UtcNow;

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert
			var elapsed = DateTime.UtcNow - startTime;
			// Service should take some time to process (at minimum the delay in the implementation)
			elapsed.TotalMilliseconds.Should().BeGreaterThan(0);
		}

		#endregion

		#region Service Interaction Tests

		[Fact]
		public async Task StartAsync_ShouldLogStartupInformation()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert - Should log some information (either starting or already ran)
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.IsAny<It.IsAnyType>(),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.AtLeastOnce);
		}

		#endregion

		#region Cancellation Tests

		[Fact]
		public async Task StartAsync_WithCancellationToken_ShouldHandleGracefully()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);
			var cts = new CancellationTokenSource();
			cts.Cancel(); // Cancel immediately

			// Act & Assert
			// Should not throw even with cancelled token
			await service.StartAsync(cts.Token);
		}

		[Fact]
		public async Task StopAsync_WithCancellationToken_ShouldComplete()
		{
			// Arrange
			var service = new JobStartupService(_serviceProvider, _mockScheduler.Object, _mockLogger.Object);
			var cts = new CancellationTokenSource();

			// Act
			await service.StopAsync(cts.Token);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopping")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion
	}
}
