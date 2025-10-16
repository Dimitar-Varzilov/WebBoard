using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests
{
	public class JobCleanupServiceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<ILogger<JobCleanupService>> _mockLogger;
		private readonly JobCleanupOptions _cleanupOptions;
		private readonly JobCleanupService _cleanupService;

		public JobCleanupServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockScheduler = new Mock<IScheduler>();
			_mockLogger = new Mock<ILogger<JobCleanupService>>();

			_cleanupOptions = new JobCleanupOptions
			{
				RemoveFromScheduler = true,
				RemoveFromDatabase = false
			};

			var mockOptions = new Mock<IOptions<JobCleanupOptions>>();
			mockOptions.Setup(o => o.Value).Returns(_cleanupOptions);

			_cleanupService = new JobCleanupService(
				_mockScheduler.Object,
				_dbContext,
				mockOptions.Object,
				_mockLogger.Object);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region CleanupCompletedJobAsync Tests

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldLogWarning_WhenJobNotFound()
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Job {jobId} not found in database")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldLogWarning_WhenJobNotCompleted()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Running, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Job {jobId} is not completed")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldRemoveFromScheduler_WhenConfigured()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockScheduler.Verify(
				s => s.CheckExists(It.Is<JobKey>(k => k.Name == jobId.ToString()), default),
				Times.Once);
			_mockScheduler.Verify(
				s => s.DeleteJob(It.Is<JobKey>(k => k.Name == jobId.ToString()), default),
				Times.Once);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldNotRemoveFromScheduler_WhenNotConfigured()
		{
			// Arrange
			_cleanupOptions.RemoveFromScheduler = false;
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockScheduler.Verify(
				s => s.CheckExists(It.IsAny<JobKey>(), default),
				Times.Never);
			_mockScheduler.Verify(
				s => s.DeleteJob(It.IsAny<JobKey>(), default),
				Times.Never);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldRemoveFromDatabase_WhenConfigured()
		{
			// Arrange
			_cleanupOptions.RemoveFromDatabase = true;
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			var removedJob = await _dbContext.Jobs.FindAsync(jobId);
			removedJob.Should().BeNull();
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldNotRemoveFromDatabase_WhenNotConfigured()
		{
			// Arrange
			_cleanupOptions.RemoveFromDatabase = false;
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			var preservedJob = await _dbContext.Jobs.FindAsync(jobId);
			preservedJob.Should().NotBeNull();
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldLogInformation_WhenDatabaseCleanupDisabled()
		{
			// Arrange
			_cleanupOptions.RemoveFromDatabase = false;
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("preserving job") &&
						v.ToString()!.Contains("audit trail")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldLogWarning_WhenDatabaseCleanupEnabled()
		{
			// Arrange
			_cleanupOptions.RemoveFromDatabase = true;
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Database cleanup is enabled")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldLogSuccess_AfterCleanup()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Successfully cleaned up completed job")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupCompletedJobAsync_ShouldThrowAndLogError_WhenExceptionOccurs()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var expectedException = new InvalidOperationException("Cleanup error");
			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default))
				.ThrowsAsync(expectedException);

			// Act
			Func<Task> act = () => _cleanupService.CleanupCompletedJobAsync(jobId);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>();
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Error cleaning up job {jobId}")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region CleanupFromSchedulerOnlyAsync Tests

		[Fact]
		public async Task CleanupFromSchedulerOnlyAsync_ShouldRemoveJobFromScheduler()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupFromSchedulerOnlyAsync(jobId);

			// Assert
			_mockScheduler.Verify(
				s => s.DeleteJob(It.Is<JobKey>(k => k.Name == jobId.ToString()), default),
				Times.Once);
		}

		[Fact]
		public async Task CleanupFromSchedulerOnlyAsync_ShouldLogInformation()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupFromSchedulerOnlyAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Removed job {jobId} from scheduler only")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupFromSchedulerOnlyAsync_ShouldThrowAndLogError_WhenExceptionOccurs()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var expectedException = new InvalidOperationException("Scheduler error");
			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default))
				.ThrowsAsync(expectedException);

			// Act
			Func<Task> act = () => _cleanupService.CleanupFromSchedulerOnlyAsync(jobId);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>();
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Error cleaning up job {jobId} from scheduler")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region CleanupAllCompletedJobsAsync Tests

		[Fact]
		public async Task CleanupAllCompletedJobsAsync_ShouldLogInformation_WhenNoJobsFound()
		{
			// Act
			await _cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("No completed jobs found")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupAllCompletedJobsAsync_ShouldCleanupMultipleJobs()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var job3 = new Job(Guid.NewGuid(), "Job3", JobStatus.Running, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.AddRange(job1, job2, job3);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			_mockScheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), default), Times.Exactly(2));
		}

		[Fact]
		public async Task CleanupAllCompletedJobsAsync_ShouldOnlyCleanupCompletedJobs()
		{
			// Arrange
			var completedJob = new Job(Guid.NewGuid(), "Completed", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var runningJob = new Job(Guid.NewGuid(), "Running", JobStatus.Running, DateTimeOffset.UtcNow, null);
			var failedJob = new Job(Guid.NewGuid(), "Failed", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.AddRange(completedJob, runningJob, failedJob);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			_mockScheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), default), Times.Once);
		}

		[Fact]
		public async Task CleanupAllCompletedJobsAsync_ShouldRemoveFromDatabase_WhenConfigured()
		{
			// Arrange
			_cleanupOptions.RemoveFromDatabase = true;
			var job1 = new Job(Guid.NewGuid(), "Job1", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.AddRange(job1, job2);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);

			// Act
			await _cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			var remainingJobs = await _dbContext.Jobs.ToListAsync();
			remainingJobs.Should().BeEmpty();
		}

		[Fact]
		public async Task CleanupAllCompletedJobsAsync_ShouldLogCompletionSummary()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.AddRange(job1, job2);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Cleanup completed:")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task CleanupAllCompletedJobsAsync_ShouldContinue_WhenIndividualJobFails()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.AddRange(job1, job2);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.SetupSequence(s => s.CheckExists(It.IsAny<JobKey>(), default))
				.ThrowsAsync(new InvalidOperationException("Error"))
				.ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act
			await _cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Cleanup completed:") &&
						v.ToString()!.Contains("1 failures")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task CleanupFlow_ShouldWorkCorrectly()
		{
			// Arrange
			var completedJob = new Job(Guid.NewGuid(), "Completed", JobStatus.Completed, DateTimeOffset.UtcNow, null);
			var runningJob = new Job(Guid.NewGuid(), "Running", JobStatus.Running, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.AddRange(completedJob, runningJob);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			// Act - Cleanup individual completed job
			await _cleanupService.CleanupCompletedJobAsync(completedJob.Id);

			// Assert - Completed job should still be in database (RemoveFromDatabase = false)
			var job = await _dbContext.Jobs.FindAsync(completedJob.Id);
			job.Should().NotBeNull();

			// Assert - Running job should be untouched
			var runningJobStillExists = await _dbContext.Jobs.FindAsync(runningJob.Id);
			runningJobStillExists.Should().NotBeNull();

			// Assert - Scheduler should have been called
			_mockScheduler.Verify(
				s => s.DeleteJob(It.Is<JobKey>(k => k.Name == completedJob.Id.ToString()), default),
				Times.Once);
		}

		#endregion
	}
}
