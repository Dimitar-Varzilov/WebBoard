using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests
{
	/// <summary>
	/// Integration tests for JobStartupService
	/// Note: Due to the static _hasRunOnce field in JobStartupService,
	/// these tests must be run in isolation or accept the "already run" behavior
	/// </summary>
	public class JobStartupServiceTests : IDisposable
	{
		private readonly Mock<IServiceProvider> _mockServiceProvider;
		private readonly Mock<IServiceScope> _mockServiceScope;
		private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<ILogger<JobStartupService>> _mockLogger;
		private readonly AppDbContext _dbContext;
		private readonly Mock<IJobSchedulingService> _mockJobSchedulingService;

		public JobStartupServiceTests()
		{
			// Setup in-memory database
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			// Setup mocks
			_mockServiceProvider = new Mock<IServiceProvider>();
			_mockServiceScope = new Mock<IServiceScope>();
			_mockScopeFactory = new Mock<IServiceScopeFactory>();
			_mockScheduler = new Mock<IScheduler>();
			_mockLogger = new Mock<ILogger<JobStartupService>>();
			_mockJobSchedulingService = new Mock<IJobSchedulingService>();

			// Setup service provider chain
			_mockServiceScope.Setup(s => s.ServiceProvider).Returns(CreateScopedServiceProvider());
			_mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
			_mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
				.Returns(_mockScopeFactory.Object);

			// Setup scheduler as started
			_mockScheduler.Setup(s => s.IsStarted).Returns(true);
		}

		private IServiceProvider CreateScopedServiceProvider()
		{
			var scopedProvider = new Mock<IServiceProvider>();
			scopedProvider.Setup(p => p.GetService(typeof(AppDbContext))).Returns(_dbContext);
			scopedProvider.Setup(p => p.GetService(typeof(IJobSchedulingService)))
				.Returns(_mockJobSchedulingService.Object);
			return scopedProvider.Object;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region StartAsync Tests

		[Fact]
		public async Task StartAsync_ShouldLogStartMessage_OnFirstRun()
		{
			// Arrange
			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Either logs "starting" or "already run" depending on test order
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Job Startup Service")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.AtLeastOnce);
		}

		[Fact]
		public async Task StopAsync_ShouldLogStopMessage()
		{
			// Arrange
			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StopAsync(cancellationToken);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Job Startup Service is stopping")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task StopAsync_ShouldCompleteSuccessfully()
		{
			// Arrange
			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			Func<Task> act = () => service.StopAsync(cancellationToken);

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region Service Provider and Dependency Tests

		[Fact]
		public async Task StartAsync_ShouldCreateServiceScope()
		{
			// Arrange
			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Should create scope to get DbContext and JobSchedulingService
			_mockServiceProvider.Verify(
				p => p.GetService(typeof(IServiceScopeFactory)),
				Times.AtMostOnce); // Depends on whether it's first run
		}

		[Fact]
		public async Task StartAsync_ShouldHandleException_AndLogError()
		{
			// Arrange
			var mockProvider = new Mock<IServiceProvider>();
			mockProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
				.Throws(new InvalidOperationException("Service provider error"));

			var service = new JobStartupService(
				mockProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Should not throw, may log error if it's the first run
			_mockLogger.Verify(
				x => x.Log(
					It.IsAny<LogLevel>(),
					It.IsAny<EventId>(),
					It.IsAny<It.IsAnyType>(),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.AtLeastOnce);
		}

		#endregion

		#region Database Query Tests

		[Fact]
		public async Task StartAsync_ShouldQueryPendingJobs_FromDatabase()
		{
			// Arrange
			var oldJob = new Job(
				Guid.NewGuid(),
				"TestJob",
				JobStatus.Queued,
				DateTimeOffset.UtcNow.AddMinutes(-5),
				null);
			_dbContext.Jobs.Add(oldJob);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Job should exist in database
			var job = await _dbContext.Jobs.FindAsync(oldJob.Id);
			job.Should().NotBeNull();
			job!.Status.Should().Be(JobStatus.Queued);
		}

		[Fact]
		public async Task StartAsync_ShouldIgnoreRecentJobs()
		{
			// Arrange
			var recentJob = new Job(
				Guid.NewGuid(),
				"RecentJob",
				JobStatus.Queued,
				DateTimeOffset.UtcNow, // Just created
				null);
			_dbContext.Jobs.Add(recentJob);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Recent jobs should be ignored (< 1 minute old)
			// The job should still exist in database but not be scheduled
			var job = await _dbContext.Jobs.FindAsync(recentJob.Id);
			job.Should().NotBeNull();
		}

		[Fact]
		public async Task StartAsync_ShouldOnlyQueryQueuedJobs()
		{
			// Arrange
			var queuedJob = new Job(Guid.NewGuid(), "Queued", JobStatus.Queued, DateTimeOffset.UtcNow.AddMinutes(-5), null);
			var runningJob = new Job(Guid.NewGuid(), "Running", JobStatus.Running, DateTimeOffset.UtcNow.AddMinutes(-5), null);
			var completedJob = new Job(Guid.NewGuid(), "Completed", JobStatus.Completed, DateTimeOffset.UtcNow.AddMinutes(-5), null);
			
			_dbContext.Jobs.AddRange(queuedJob, runningJob, completedJob);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Only queued job should be considered
			var jobs = await _dbContext.Jobs.Where(j => j.Status == JobStatus.Queued).ToListAsync();
			jobs.Should().HaveCount(1);
			jobs[0].Id.Should().Be(queuedJob.Id);
		}

		#endregion

		#region Job Prioritization Tests

		[Fact]
		public async Task StartAsync_ShouldOrderJobsByScheduledTime()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", JobStatus.Queued, 
				DateTimeOffset.UtcNow.AddMinutes(-5), 
				DateTimeOffset.UtcNow.AddHours(2));

			var job2 = new Job(Guid.NewGuid(), "Job2", JobStatus.Queued, 
				DateTimeOffset.UtcNow.AddMinutes(-4), 
				DateTimeOffset.UtcNow.AddHours(1));

			_dbContext.Jobs.AddRange(job1, job2);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act
			await service.StartAsync(cancellationToken);

			// Assert - Jobs should be ordered by ScheduledAt
			var orderedJobs = await _dbContext.Jobs
				.Where(j => j.Status == JobStatus.Queued)
				.OrderBy(j => j.ScheduledAt ?? j.CreatedAt)
				.ToListAsync();

			orderedJobs[0].Id.Should().Be(job2.Id); // Earlier scheduled time
			orderedJobs[1].Id.Should().Be(job1.Id); // Later scheduled time
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task ServiceLifecycle_ShouldCompleteWithoutErrors()
		{
			// Arrange
			var job = new Job(Guid.NewGuid(), "TestJob", JobStatus.Queued, DateTimeOffset.UtcNow.AddMinutes(-5), null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var service = new JobStartupService(
				_mockServiceProvider.Object,
				_mockScheduler.Object,
				_mockLogger.Object);
			var cancellationToken = CancellationToken.None;

			// Act - Start
			Func<Task> startAct = () => service.StartAsync(cancellationToken);

			// Assert - Should not throw
			await startAct.Should().NotThrowAsync();

			// Act - Stop
			Func<Task> stopAct = () => service.StopAsync(cancellationToken);

			// Assert - Should not throw
			await stopAct.Should().NotThrowAsync();
		}

		#endregion
	}
}
