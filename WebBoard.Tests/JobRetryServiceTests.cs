using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests
{
	public class JobRetryServiceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IJobSchedulingService> _mockSchedulingService;
		private readonly Mock<ILogger<JobRetryService>> _mockLogger;
		private readonly JobRetryService _retryService;

		public JobRetryServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockSchedulingService = new Mock<IJobSchedulingService>();
			_mockLogger = new Mock<ILogger<JobRetryService>>();
			_retryService = new JobRetryService(_dbContext, _mockSchedulingService.Object, _mockLogger.Object);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region ShouldRetryJobAsync Tests

		[Fact]
		public async Task ShouldRetryJobAsync_ShouldReturnTrue_WhenNoRetryInfoExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			var result = await _retryService.ShouldRetryJobAsync(jobId);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task ShouldRetryJobAsync_ShouldReturnTrue_WhenRetryCountBelowMax()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 1,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"Error",
				DateTimeOffset.UtcNow);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _retryService.ShouldRetryJobAsync(jobId);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task ShouldRetryJobAsync_ShouldReturnFalse_WhenRetryCountEqualsMax()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 3,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"Error",
				DateTimeOffset.UtcNow);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _retryService.ShouldRetryJobAsync(jobId);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task ShouldRetryJobAsync_ShouldReturnFalse_WhenRetryCountExceedsMax()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 5,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"Error",
				DateTimeOffset.UtcNow);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _retryService.ShouldRetryJobAsync(jobId);

			// Assert
			result.Should().BeFalse();
		}

		#endregion

		#region ScheduleRetryAsync Tests

		[Fact]
		public async Task ScheduleRetryAsync_ShouldCreateRetryInfo_WhenFirstRetry()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();
			var errorMessage = "Test error";

			// Act
			await _retryService.ScheduleRetryAsync(jobId, errorMessage);

			// Assert
			var retryInfo = await _dbContext.JobRetries.FirstOrDefaultAsync(r => r.JobId == jobId);
			retryInfo.Should().NotBeNull();
			retryInfo!.RetryCount.Should().Be(0);
			retryInfo.MaxRetries.Should().Be(3);
			retryInfo.LastErrorMessage.Should().Be(errorMessage);
			retryInfo.NextRetryAt.Should().BeAfter(DateTimeOffset.UtcNow);
		}

		[Fact]
		public async Task ScheduleRetryAsync_ShouldUpdateRetryInfo_WhenSubsequentRetry()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 0,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"First error",
				DateTimeOffset.UtcNow);
			_dbContext.Jobs.Add(job);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();
			var newError = "Second error";

			// Act
			await _retryService.ScheduleRetryAsync(jobId, newError);

			// Assert
			var updated = await _dbContext.JobRetries.FirstOrDefaultAsync(r => r.JobId == jobId);
			updated.Should().NotBeNull();
			updated!.RetryCount.Should().Be(1);
			updated.LastErrorMessage.Should().Be(newError);
		}

		[Fact]
		public async Task ScheduleRetryAsync_ShouldScheduleJob_WhenJobExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _retryService.ScheduleRetryAsync(jobId, "Error");

			// Assert
			_mockSchedulingService.Verify(
				x => x.ScheduleJobAsync(It.Is<Job>(j => j.Id == jobId)),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleRetryAsync_ShouldLogCreation_WhenFirstRetry()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act
			await _retryService.ScheduleRetryAsync(jobId, "Error");

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Created retry tracking for job {jobId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleRetryAsync_ShouldLogError_WhenJobNotFound()
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			await _retryService.ScheduleRetryAsync(jobId, "Error");

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Job {jobId} not found")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleRetryAsync_ShouldIncrementRetryCount()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act & Assert - First retry
			await _retryService.ScheduleRetryAsync(jobId, "Error 1");
			_dbContext.ChangeTracker.Clear();
			var retry1 = await _dbContext.JobRetries.FirstAsync(r => r.JobId == jobId);
			retry1.RetryCount.Should().Be(0);
			retry1.LastErrorMessage.Should().Be("Error 1");

			// Act & Assert - Second retry
			await _retryService.ScheduleRetryAsync(jobId, "Error 2");
			_dbContext.ChangeTracker.Clear();
			var retry2 = await _dbContext.JobRetries.FirstAsync(r => r.JobId == jobId);
			retry2.RetryCount.Should().Be(1);
			retry2.LastErrorMessage.Should().Be("Error 2");

			// Act & Assert - Third retry
			await _retryService.ScheduleRetryAsync(jobId, "Error 3");
			_dbContext.ChangeTracker.Clear();
			var retry3 = await _dbContext.JobRetries.FirstAsync(r => r.JobId == jobId);
			retry3.RetryCount.Should().Be(2);
			retry3.LastErrorMessage.Should().Be("Error 3");

			// Verify MaxRetries is consistent
			retry3.MaxRetries.Should().Be(3);
		}

		#endregion

		#region GetRetryInfoAsync Tests

		[Fact]
		public async Task GetRetryInfoAsync_ShouldReturnRetryInfo_WhenExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 1,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"Error",
				DateTimeOffset.UtcNow);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _retryService.GetRetryInfoAsync(jobId);

			// Assert
			result.Should().NotBeNull();
			result!.JobId.Should().Be(jobId);
			result.RetryCount.Should().Be(1);
			result.MaxRetries.Should().Be(3);
		}

		[Fact]
		public async Task GetRetryInfoAsync_ShouldReturnNull_WhenNotExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			var result = await _retryService.GetRetryInfoAsync(jobId);

			// Assert
			result.Should().BeNull();
		}

		#endregion

		#region RemoveRetryInfoAsync Tests

		[Fact]
		public async Task RemoveRetryInfoAsync_ShouldRemoveRetryInfo_WhenExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 1,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"Error",
				DateTimeOffset.UtcNow);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();

			// Act
			await _retryService.RemoveRetryInfoAsync(jobId);

			// Assert
			var result = await _dbContext.JobRetries.FirstOrDefaultAsync(r => r.JobId == jobId);
			result.Should().BeNull();
		}

		[Fact]
		public async Task RemoveRetryInfoAsync_ShouldLogInformation_WhenExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var retryInfo = new JobRetryInfo(
				Guid.NewGuid(),
				jobId,
				RetryCount: 1,
				MaxRetries: 3,
				DateTimeOffset.UtcNow.AddMinutes(1),
				"Error",
				DateTimeOffset.UtcNow);
			_dbContext.JobRetries.Add(retryInfo);
			await _dbContext.SaveChangesAsync();

			// Act
			await _retryService.RemoveRetryInfoAsync(jobId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Removed retry tracking for job {jobId}")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task RemoveRetryInfoAsync_ShouldNotThrow_WhenNotExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();

			// Act
			Func<Task> act = () => _retryService.RemoveRetryInfoAsync(jobId);

			// Assert
			await act.Should().NotThrowAsync();
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task RetryLifecycle_ShouldWorkCorrectly()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, "TestJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			// Act & Assert - Should retry (no info exists)
			var shouldRetry1 = await _retryService.ShouldRetryJobAsync(jobId);
			shouldRetry1.Should().BeTrue();

			// Act & Assert - Schedule first retry
			await _retryService.ScheduleRetryAsync(jobId, "Error 1");
			var retry1 = await _retryService.GetRetryInfoAsync(jobId);
			retry1.Should().NotBeNull();
			retry1!.RetryCount.Should().Be(0);

			// Act & Assert - Schedule second retry
			await _retryService.ScheduleRetryAsync(jobId, "Error 2");
			var retry2 = await _retryService.GetRetryInfoAsync(jobId);
			retry2!.RetryCount.Should().Be(1);

			// Act & Assert - Schedule third retry
			await _retryService.ScheduleRetryAsync(jobId, "Error 3");
			var retry3 = await _retryService.GetRetryInfoAsync(jobId);
			retry3!.RetryCount.Should().Be(2);

			// Act & Assert - Should still retry (count = 2, max = 3)
			var shouldRetry2 = await _retryService.ShouldRetryJobAsync(jobId);
			shouldRetry2.Should().BeTrue();

			// Act & Assert - Schedule fourth retry (reaches max)
			await _retryService.ScheduleRetryAsync(jobId, "Error 4");
			var retry4 = await _retryService.GetRetryInfoAsync(jobId);
			retry4!.RetryCount.Should().Be(3);

			// Act & Assert - Should NOT retry (count = 3, max = 3)
			var shouldRetry3 = await _retryService.ShouldRetryJobAsync(jobId);
			shouldRetry3.Should().BeFalse();

			// Act & Assert - Remove retry info
			await _retryService.RemoveRetryInfoAsync(jobId);
			var final = await _retryService.GetRetryInfoAsync(jobId);
			final.Should().BeNull();
		}

		#endregion
	}
}
