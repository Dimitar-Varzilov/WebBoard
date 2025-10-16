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

namespace WebBoard.Tests.Jobs
{
	public class BaseJobTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IServiceProvider> _mockServiceProvider;
		private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
		private readonly Mock<IServiceScope> _mockScope;
		private readonly Mock<IServiceProvider> _mockScopedProvider;
		private readonly Mock<IJobCleanupService> _mockCleanupService;
		private readonly Mock<IJobStatusNotifier> _mockStatusNotifier;
		private readonly Mock<IJobRetryService> _mockRetryService;
		private readonly Mock<ILogger<TestJob>> _mockLogger;

		public BaseJobTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockScopeFactory = new Mock<IServiceScopeFactory>();
			_mockScope = new Mock<IServiceScope>();
			_mockScopedProvider = new Mock<IServiceProvider>();
			_mockCleanupService = new Mock<IJobCleanupService>();
			_mockStatusNotifier = new Mock<IJobStatusNotifier>();
			_mockRetryService = new Mock<IJobRetryService>();
			_mockLogger = new Mock<ILogger<TestJob>>();

			// Setup service provider chain - create a service provider that returns the scope factory
			_mockServiceProvider = new Mock<IServiceProvider>();
			_mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
				.Returns(_mockScopeFactory.Object);

			// Setup scope factory to return scope
			_mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
			_mockScope.SetupGet(s => s.ServiceProvider).Returns(_mockScopedProvider.Object);

			_mockScopedProvider.Setup(sp => sp.GetService(typeof(AppDbContext))).Returns(_dbContext);
			_mockScopedProvider.Setup(sp => sp.GetService(typeof(IJobCleanupService))).Returns(_mockCleanupService.Object);
			_mockScopedProvider.Setup(sp => sp.GetService(typeof(IJobStatusNotifier))).Returns(_mockStatusNotifier.Object);
			_mockScopedProvider.Setup(sp => sp.GetService(typeof(IJobRetryService))).Returns(_mockRetryService.Object);
		}

		public void Dispose()
		{
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
			GC.SuppressFinalize(this);
		}

		[Fact]
		public async Task Execute_WithSuccessfulJob_ShouldUpdateStatusToCompleted()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Queued, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await testJob.Execute(context);

			// Assert
			var updatedJob = await _dbContext.Jobs.FindAsync(jobId);
			updatedJob!.Status.Should().Be(JobStatus.Completed);

			_mockStatusNotifier.Verify(s => s.NotifyJobStatusAsync(
				jobId, "TestJob", JobStatus.Running, null), Times.Once);
			_mockStatusNotifier.Verify(s => s.NotifyJobStatusAsync(
				jobId, "TestJob", JobStatus.Completed, null), Times.Once);
			_mockCleanupService.Verify(c => c.CleanupCompletedJobAsync(jobId), Times.Once);
		}

		[Fact]
		public async Task Execute_WithFailedJob_ShouldScheduleRetry()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Queued, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: false, errorMessage: "Test error");
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.ShouldRetryJobAsync(jobId)).ReturnsAsync(true);
			_mockRetryService.Setup(r => r.ScheduleRetryAsync(jobId, "Test error")).Returns(Task.CompletedTask);

			// Act
			await testJob.Execute(context);

			// Assert
			_mockRetryService.Verify(r => r.ScheduleRetryAsync(jobId, "Test error"), Times.Once);
			_mockCleanupService.Verify(c => c.CleanupCompletedJobAsync(jobId), Times.Once);
		}

		[Fact]
		public async Task Execute_WithException_ShouldHandleRetry()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Queued, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, throwException: true);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.ShouldRetryJobAsync(jobId)).ReturnsAsync(true);
			_mockRetryService.Setup(r => r.ScheduleRetryAsync(jobId, It.IsAny<string>())).Returns(Task.CompletedTask);

			// Act
			await testJob.Execute(context);

			// Assert
			_mockRetryService.Verify(r => r.ShouldRetryJobAsync(jobId), Times.AtLeast(1));
			_mockRetryService.Verify(r => r.ScheduleRetryAsync(jobId, It.IsAny<string>()), Times.Once);
		}

		[Fact]
		public async Task Execute_WhenRetryLimitReached_ShouldMarkAsFailed()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Queued, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, throwException: true);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.ShouldRetryJobAsync(jobId)).ReturnsAsync(false);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(() => testJob.Execute(context));

			var updatedJob = await _dbContext.Jobs.FindAsync(jobId);
			updatedJob!.Status.Should().Be(JobStatus.Failed);
			_mockRetryService.Verify(r => r.RemoveRetryInfoAsync(jobId), Times.Once);
		}

		[Fact]
		public async Task Execute_WithRetryInfo_ShouldLogAttemptNumber()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Queued, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				2, // Retry count
				3,
				DateTimeOffset.UtcNow.AddMinutes(5),
				"Previous error",
				DateTimeOffset.UtcNow.AddMinutes(-10));

			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync(retryInfo);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await testJob.Execute(context);

			// Assert - Should be attempt 3 (retryCount + 1)
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempt: 3/3")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.AtLeastOnce);
		}

		[Fact]
		public async Task Execute_WhenJobNotFound_ShouldLogErrorAndReturn()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);
			var context = CreateJobExecutionContext(jobId);

			// Act
			await testJob.Execute(context);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ShouldRetryOnError_WithValidationError_ShouldReturnFalse()
		{
			// Arrange
			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);

			// Act & Assert
			testJob.TestShouldRetryOnError("Validation failed").Should().BeFalse();
			testJob.TestShouldRetryOnError("validation error occurred").Should().BeFalse();
		}

		[Fact]
		public async Task ShouldRetryOnError_WithNotFoundError_ShouldReturnFalse()
		{
			// Arrange
			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);

			// Act & Assert
			testJob.TestShouldRetryOnError("Resource not found").Should().BeFalse();
			testJob.TestShouldRetryOnError("NOT FOUND").Should().BeFalse();
		}

		[Fact]
		public async Task ShouldRetryOnError_WithOtherErrors_ShouldReturnTrue()
		{
			// Arrange
			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);

			// Act & Assert
			testJob.TestShouldRetryOnError("Network timeout").Should().BeTrue();
			testJob.TestShouldRetryOnError("Database connection failed").Should().BeTrue();
			testJob.TestShouldRetryOnError(null).Should().BeTrue();
		}

		[Fact]
		public async Task UpdateJobTasksOnCompletion_ShouldUpdatePendingTasks()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Running, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc 1", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc 2", TaskItemStatus.InProgress, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc 3", TaskItemStatus.Completed, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			var testJob = new TestJob(_mockServiceProvider.Object, _mockLogger.Object, isSuccess: true);

			// Act
			var count = await testJob.TestUpdateJobTasksOnCompletion(_dbContext, jobId, CancellationToken.None);

			// Assert
			count.Should().Be(2); // Only pending and in-progress should be updated

			var updatedTasks = await _dbContext.Tasks.Where(t => t.JobId == jobId).ToListAsync();
			updatedTasks.Where(t => t.Status == TaskItemStatus.Completed).Should().HaveCount(3);
		}

		private static IJobExecutionContext CreateJobExecutionContext(Guid jobId)
		{
			var mockContext = new Mock<IJobExecutionContext>();
			var jobDataMap = new JobDataMap();
			jobDataMap.Put("JobId", jobId);

			mockContext.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
			mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

			return mockContext.Object;
		}

		// Test implementation of BaseJob
		public class TestJob(
			IServiceProvider serviceProvider,
			ILogger logger,
			bool isSuccess = true,
			string? errorMessage = null,
			bool throwException = false) : BaseJob(serviceProvider, logger)
		{
			protected override async Task<JobExecutionResult> ExecuteJobLogic(
				IServiceProvider scopedServices,
				AppDbContext dbContext,
				Guid jobId,
				CancellationToken cancellationToken)
			{
				await Task.CompletedTask;

				return throwException ? throw new InvalidOperationException("Test exception") : new JobExecutionResult(isSuccess, 5, errorMessage);
			}

			// Expose protected methods for testing
			public bool TestShouldRetryOnError(string? errorMessage)
			{
				return ShouldRetryOnError(errorMessage);
			}

			public Task<int> TestUpdateJobTasksOnCompletion(AppDbContext dbContext, Guid jobId, CancellationToken ct)
			{
				return UpdateJobTasksOnCompletionAsync(dbContext, jobId, ct);
			}
		}
	}
}
