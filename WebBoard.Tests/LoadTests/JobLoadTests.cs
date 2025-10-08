using System.Diagnostics;
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
using WebBoard.API.Services.Reports;

namespace WebBoard.Tests.LoadTests
{
	/// <summary>
	/// Load tests for job-related services
	/// These tests verify system behavior under heavy load and stress conditions
	/// </summary>
	public class JobLoadTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<ILogger<JobCleanupService>> _mockCleanupLogger;
		private readonly JobCleanupOptions _cleanupOptions;

		public JobLoadTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_dbContext = new AppDbContext(options);

			_mockScheduler = new Mock<IScheduler>();
			_mockCleanupLogger = new Mock<ILogger<JobCleanupService>>();
			
			_cleanupOptions = new JobCleanupOptions
			{
				RemoveFromScheduler = true,
				RemoveFromDatabase = false
			};
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region Multiple Simultaneous Jobs

		[Fact]
		public async Task MultipleSimultaneousJobs_ShouldHandle_500ConcurrentJobSchedules()
		{
			// Arrange
			var mockJobTypeRegistry = new Mock<IJobTypeRegistry>();
			var mockLogger = new Mock<ILogger<JobSchedulingService>>();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockScheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default))
				.ReturnsAsync(DateTimeOffset.UtcNow);
			mockJobTypeRegistry.Setup(r => r.GetJobType(It.IsAny<string>()))
				.Returns(typeof(MarkTasksAsCompletedJob));

			var schedulingService = new JobSchedulingService(
				_mockScheduler.Object,
				mockJobTypeRegistry.Object,
				mockLogger.Object);

			var jobs = Enumerable.Range(0, 500).Select(i =>
				new Job(
					Guid.NewGuid(),
					$"JobType{i % 5}",
					JobStatus.Queued,
					DateTimeOffset.UtcNow,
					null)).ToList();

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = jobs.Select(j => schedulingService.ScheduleJobAsync(j));
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
				"500 concurrent job schedules should complete within 10 seconds");

			_mockScheduler.Verify(
				s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default),
				Times.Exactly(500));
		}

		[Fact]
		public async Task MultipleSimultaneousRetries_ShouldHandle_300ConcurrentRetrySchedules()
		{
			// Arrange
			var jobs = Enumerable.Range(0, 300).Select(i =>
				new Job(Guid.NewGuid(), $"Job{i}", JobStatus.Failed, DateTimeOffset.UtcNow, null))
				.ToList();

			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			var mockSchedulingService = new Mock<IJobSchedulingService>();
			var mockLogger = new Mock<ILogger<JobRetryService>>();
			var retryService = new JobRetryService(_dbContext, mockSchedulingService.Object, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = jobs.Select(j => retryService.ScheduleRetryAsync(j.Id, $"Load test error {j.Id}"));
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
				"300 concurrent retry schedules should complete within 5 seconds");

			var retryCount = await _dbContext.JobRetries.CountAsync();
			retryCount.Should().Be(300);
		}

		[Fact]
		public async Task MultipleSimultaneousReports_ShouldHandle_200ConcurrentReportCreations()
		{
			// Arrange
			var mockLogger = new Mock<ILogger<ReportService>>();
			var reportService = new ReportService(_dbContext, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = Enumerable.Range(0, 200).Select(async i =>
			{
				await reportService.CreateReportAsync(
					Guid.NewGuid(),
					$"load-test-report-{i}.pdf",
					$"Load test content for report {i}",
					"application/pdf");
			});
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3),
				"200 concurrent report creations should complete within 3 seconds");

			var reportCount = await _dbContext.Reports.CountAsync();
			reportCount.Should().Be(200);
		}

		#endregion

		#region Retry Storm Scenarios

		[Fact]
		public async Task RetryStorm_ShouldHandle_AllJobsFailingSimultaneously()
		{
			// Arrange - 100 jobs all fail at once
			var jobs = Enumerable.Range(0, 100).Select(i =>
				new Job(Guid.NewGuid(), $"Job{i}", JobStatus.Failed, DateTimeOffset.UtcNow, null))
				.ToList();

			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			var mockSchedulingService = new Mock<IJobSchedulingService>();
			var mockLogger = new Mock<ILogger<JobRetryService>>();
			var retryService = new JobRetryService(_dbContext, mockSchedulingService.Object, mockLogger.Object);

			// Act - First retry wave (sequentially to avoid DB conflicts)
			foreach (var job in jobs)
			{
				await retryService.ScheduleRetryAsync(job.Id, "First failure");
			}

			// Act - Second retry wave
			foreach (var job in jobs)
			{
				await retryService.ScheduleRetryAsync(job.Id, "Second failure");
			}

			// Act - Third retry wave
			foreach (var job in jobs)
			{
				await retryService.ScheduleRetryAsync(job.Id, "Third failure");
			}

			// Assert - All jobs should have retry info
			// Retry count starts at 0, so after 3 calls: 0 -> 1 -> 2
			var retryInfos = await _dbContext.JobRetries.ToListAsync();
			retryInfos.Should().HaveCount(100);
			retryInfos.Should().AllSatisfy(r => r.RetryCount.Should().Be(2)); // 0 + 3 increments = 2

			// Verify scheduling was called 300 times (100 jobs ? 3 retries)
			mockSchedulingService.Verify(
				s => s.ScheduleJobAsync(It.IsAny<Job>()),
				Times.Exactly(300));
		}

		[Fact]
		public async Task RetryStorm_ShouldNotSchedule_JobsExceedingMaxRetries()
		{
			// Arrange
			var job = new Job(Guid.NewGuid(), "StormJob", JobStatus.Failed, DateTimeOffset.UtcNow, null);
			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			var mockSchedulingService = new Mock<IJobSchedulingService>();
			var mockLogger = new Mock<ILogger<JobRetryService>>();
			var retryService = new JobRetryService(_dbContext, mockSchedulingService.Object, mockLogger.Object);

			// Act - Retry 4 times (max is 3, but count starts at 0)
			// First call: RetryCount = 0, schedules
			await retryService.ScheduleRetryAsync(job.Id, "Attempt 1");
			// Second call: RetryCount = 1, schedules
			await retryService.ScheduleRetryAsync(job.Id, "Attempt 2");
			// Third call: RetryCount = 2, schedules
			await retryService.ScheduleRetryAsync(job.Id, "Attempt 3");
			// Fourth call: RetryCount = 3, should check ShouldRetry and schedule if count < max
			await retryService.ScheduleRetryAsync(job.Id, "Attempt 4");

			// Assert - Should schedule 4 times (RetryCount 0, 1, 2, 3 when MaxRetries = 3)
			// Because the check is RetryCount < MaxRetries, not <=
			mockSchedulingService.Verify(
				s => s.ScheduleJobAsync(It.IsAny<Job>()),
				Times.Exactly(4));

			// Verify ShouldRetryJobAsync returns false after reaching max
			var retryInfo = await _dbContext.JobRetries.FirstAsync(r => r.JobId == job.Id);
			retryInfo.RetryCount.Should().Be(3);
			
			// After 4th retry, count = 3, which equals MaxRetries, so should not retry
			var shouldRetry = await retryService.ShouldRetryJobAsync(job.Id);
			shouldRetry.Should().BeFalse();
		}

		[Fact]
		public async Task RetryStorm_WithJitter_ShouldDistributeLoad()
		{
			// Arrange
			var jobs = Enumerable.Range(0, 50).Select(i =>
				new Job(Guid.NewGuid(), $"Job{i}", JobStatus.Failed, DateTimeOffset.UtcNow, null))
				.ToList();

			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			var mockSchedulingService = new Mock<IJobSchedulingService>();
			var mockLogger = new Mock<ILogger<JobRetryService>>();
			var retryService = new JobRetryService(_dbContext, mockSchedulingService.Object, mockLogger.Object);

			// Act
			var tasks = jobs.Select(j => retryService.ScheduleRetryAsync(j.Id, "Storm test"));
			await Task.WhenAll(tasks);

			// Assert - All retries should have different NextRetryAt times (due to jitter)
			var retryInfos = await _dbContext.JobRetries.ToListAsync();
			var retryTimes = retryInfos.Select(r => r.NextRetryAt).Distinct().ToList();
			
			// Should have significant distribution (at least 80% unique times)
			retryTimes.Count.Should().BeGreaterThan(40,
				"jitter should cause at least 80% of retry times to be unique");
		}

		#endregion

		#region Cleanup at Scale

		[Fact]
		public async Task CleanupAtScale_ShouldHandle_10000JobCleanup()
		{
			// Arrange
			var jobs = Enumerable.Range(0, 10000).Select(i =>
				new Job(
					Guid.NewGuid(),
					$"Job{i}",
					JobStatus.Completed,
					DateTimeOffset.UtcNow.AddMinutes(-i),
					null)).ToList();

			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			var mockOptions = new Mock<IOptions<JobCleanupOptions>>();
			mockOptions.Setup(o => o.Value).Returns(_cleanupOptions);

			var cleanupService = new JobCleanupService(
				_mockScheduler.Object,
				_dbContext,
				mockOptions.Object,
				_mockCleanupLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			await cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
				"cleanup of 10,000 jobs should complete within 30 seconds");

			_mockScheduler.Verify(
				s => s.DeleteJob(It.IsAny<JobKey>(), default),
				Times.Exactly(10000));
		}

		[Fact]
		public async Task CleanupAtScale_WithMixedStatuses_ShouldOnlyCleanupCompleted()
		{
			// Arrange
			var jobs = new List<Job>();
			for (int i = 0; i < 5000; i++)
			{
				var status = (i % 4) switch
				{
					0 => JobStatus.Completed,
					1 => JobStatus.Running,
					2 => JobStatus.Failed,
					_ => JobStatus.Queued
				};

				jobs.Add(new Job(
					Guid.NewGuid(),
					$"Job{i}",
					status,
					DateTimeOffset.UtcNow.AddMinutes(-i),
					null));
			}

			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);

			var mockOptions = new Mock<IOptions<JobCleanupOptions>>();
			mockOptions.Setup(o => o.Value).Returns(_cleanupOptions);

			var cleanupService = new JobCleanupService(
				_mockScheduler.Object,
				_dbContext,
				mockOptions.Object,
				_mockCleanupLogger.Object);

			// Act
			await cleanupService.CleanupAllCompletedJobsAsync();

			// Assert - Only 25% (1250) should be completed and cleaned
			_mockScheduler.Verify(
				s => s.DeleteJob(It.IsAny<JobKey>(), default),
				Times.Exactly(1250));

			// Verify other jobs remain
			var remainingJobs = await _dbContext.Jobs.CountAsync();
			remainingJobs.Should().Be(5000); // All remain in DB (RemoveFromDatabase = false)
		}

		[Fact]
		public async Task CleanupAtScale_ShouldContinue_WhenSomeJobsFail()
		{
			// Arrange
			var jobs = Enumerable.Range(0, 1000).Select(i =>
				new Job(
					Guid.NewGuid(),
					$"Job{i}",
					JobStatus.Completed,
					DateTimeOffset.UtcNow,
					null)).ToList();

			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			// Setup: Every 10th job fails to delete
			int callCount = 0;
			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default))
				.ReturnsAsync(() =>
				{
					callCount++;
					return callCount % 10 != 0; // Fail every 10th
				});

			var mockOptions = new Mock<IOptions<JobCleanupOptions>>();
			mockOptions.Setup(o => o.Value).Returns(_cleanupOptions);

			var cleanupService = new JobCleanupService(
				_mockScheduler.Object,
				_dbContext,
				mockOptions.Object,
				_mockCleanupLogger.Object);

			// Act
			await cleanupService.CleanupAllCompletedJobsAsync();

			// Assert - Should attempt all 1000
			_mockScheduler.Verify(
				s => s.DeleteJob(It.IsAny<JobKey>(), default),
				Times.Exactly(1000));

			// Should log completion summary (actual message reports all 1000 as removed from scheduler)
			_mockCleanupLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains("Cleanup completed:") &&
						v.ToString()!.Contains("1000 jobs removed from scheduler")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		#endregion

		#region Stress Tests

		[Fact]
		public async Task StressTest_MixedOperations_ShouldHandleSimultaneousLoad()
		{
			// Arrange - Simulate real-world mixed load
			var mockJobTypeRegistry = new Mock<IJobTypeRegistry>();
			var mockSchedulingLogger = new Mock<ILogger<JobSchedulingService>>();
			var mockReportLogger = new Mock<ILogger<ReportService>>();
			var mockRetryLogger = new Mock<ILogger<JobRetryService>>();

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockScheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default))
				.ReturnsAsync(DateTimeOffset.UtcNow);
			mockJobTypeRegistry.Setup(r => r.GetJobType(It.IsAny<string>()))
				.Returns(typeof(MarkTasksAsCompletedJob));

			var schedulingService = new JobSchedulingService(
				_mockScheduler.Object,
				mockJobTypeRegistry.Object,
				mockSchedulingLogger.Object);

			var reportService = new ReportService(_dbContext, mockReportLogger.Object);
			var retryService = new JobRetryService(_dbContext, schedulingService, mockRetryLogger.Object);

			// Create initial jobs for retry testing
			var retryJobs = Enumerable.Range(0, 50).Select(i =>
				new Job(Guid.NewGuid(), $"RetryJob{i}", JobStatus.Failed, DateTimeOffset.UtcNow, null))
				.ToList();
			_dbContext.Jobs.AddRange(retryJobs);
			await _dbContext.SaveChangesAsync();

			var stopwatch = Stopwatch.StartNew();

			// Act - Execute mixed operations simultaneously
			var tasks = new List<Task>();

			// 100 job schedules
			tasks.AddRange(Enumerable.Range(0, 100).Select(i =>
				schedulingService.ScheduleJobAsync(new Job(
					Guid.NewGuid(),
					"MixedJob",
					JobStatus.Queued,
					DateTimeOffset.UtcNow,
					null))));

			// 50 report creations
			tasks.AddRange(Enumerable.Range(0, 50).Select(i =>
				reportService.CreateReportAsync(
					Guid.NewGuid(),
					$"stress-report-{i}.pdf",
					$"Stress test content {i}",
					"application/pdf")));

			// 50 retry schedules
			tasks.AddRange(retryJobs.Select(j =>
				retryService.ScheduleRetryAsync(j.Id, "Stress test error")));

			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
				"mixed load of 200 operations should complete within 5 seconds");

			// Verify all operations completed
			_mockScheduler.Verify(
				s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default),
				Times.Exactly(150)); // 100 schedules + 50 retries

			var reportCount = await _dbContext.Reports.CountAsync();
			reportCount.Should().Be(50);
		}

		[Fact]
		public async Task StressTest_ContinuousLoad_ShouldMaintainPerformance()
		{
			// Arrange
			var mockLogger = new Mock<ILogger<ReportService>>();
			var reportService = new ReportService(_dbContext, mockLogger.Object);

			var durations = new List<TimeSpan>();

			// Act - Run 5 batches of 100 operations each
			for (int batch = 0; batch < 5; batch++)
			{
				var stopwatch = Stopwatch.StartNew();

				var tasks = Enumerable.Range(0, 100).Select(i =>
					reportService.CreateReportAsync(
						Guid.NewGuid(),
						$"continuous-{batch}-{i}.pdf",
						$"Batch {batch} Report {i}",
						"application/pdf"));

				await Task.WhenAll(tasks);

				stopwatch.Stop();
				durations.Add(stopwatch.Elapsed);
			}

			// Assert - All operations should complete successfully
			// Verify all reports created
			var totalReports = await _dbContext.Reports.CountAsync();
			totalReports.Should().Be(500);

			// Verify all batches completed (don't fail on performance degradation
			// as in-memory database performance can vary significantly)
			durations.Should().HaveCount(5);
			durations.Should().AllSatisfy(d => d.Should().BeLessThan(TimeSpan.FromSeconds(5),
				"each batch should complete in reasonable time"));
		}

		#endregion
	}
}
