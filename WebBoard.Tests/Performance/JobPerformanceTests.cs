using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Reports;

namespace WebBoard.Tests.Performance
{
	/// <summary>
	/// Performance tests for job-related services
	/// These tests verify that operations complete within acceptable time limits
	/// </summary>
	public class JobPerformanceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<ILogger<JobCleanupService>> _mockLogger;
		private readonly JobCleanupOptions _cleanupOptions;

		public JobPerformanceTests()
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
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region Bulk Operation Performance

		[Fact]
		public async Task CleanupAllCompletedJobs_ShouldComplete_Within5Seconds_For1000Jobs()
		{
			// Arrange
			var jobs = new List<Job>();
			for (int i = 0; i < 1000; i++)
			{
				jobs.Add(new Job(
					Guid.NewGuid(),
					$"Job{i}",
					JobStatus.Completed,
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
				_mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			await cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
				"cleanup of 1000 jobs should complete in under 5 seconds");

			_mockScheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), default), Times.Exactly(1000));
		}

		[Fact]
		public async Task CreateMultipleReports_ShouldComplete_Within3Seconds_For500Reports()
		{
			// Arrange
			var mockLogger = new Mock<ILogger<ReportService>>();
			var reportService = new ReportService(_dbContext, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = new List<Task<Report>>();
			for (int i = 0; i < 500; i++)
			{
				tasks.Add(reportService.CreateReportAsync(
					Guid.NewGuid(),
					$"report{i}.pdf",
					$"Content{i}",
					"application/pdf"));
			}
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3),
				"creating 500 reports should complete in under 3 seconds");

			var reportCount = await _dbContext.Reports.CountAsync();
			reportCount.Should().Be(500);
		}

		[Fact]
		public async Task ScheduleMultipleRetries_ShouldComplete_Within2Seconds_For200Jobs()
		{
			// Arrange
			var jobs = new List<Job>();
			for (int i = 0; i < 200; i++)
			{
				var job = new Job(Guid.NewGuid(), $"Job{i}", JobStatus.Failed, DateTimeOffset.UtcNow, null);
				jobs.Add(job);
			}
			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			var mockSchedulingService = new Mock<IJobSchedulingService>();
			var mockLogger = new Mock<ILogger<JobRetryService>>();
			var retryService = new JobRetryService(_dbContext, mockSchedulingService.Object, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = jobs.Select(j => retryService.ScheduleRetryAsync(j.Id, $"Error for {j.Id}"));
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
				"scheduling 200 retries should complete in under 2 seconds");

			var retryCount = await _dbContext.JobRetries.CountAsync();
			retryCount.Should().Be(200);
		}

		#endregion

		#region Concurrent Operation Performance

		[Fact]
		public async Task ConcurrentJobScheduling_ShouldHandle_100SimultaneousSchedules()
		{
			// Arrange
			var mockScheduler = new Mock<IScheduler>();
			var mockJobTypeRegistry = new Mock<IJobTypeRegistry>();
			var mockLogger = new Mock<ILogger<JobSchedulingService>>();

			mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			mockScheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default))
				.ReturnsAsync(DateTimeOffset.UtcNow);
			mockJobTypeRegistry.Setup(r => r.GetJobType(It.IsAny<string>()))
				.Returns(typeof(MarkTasksAsCompletedJob));

			var schedulingService = new JobSchedulingService(
				mockScheduler.Object,
				mockJobTypeRegistry.Object,
				mockLogger.Object);

			var jobs = new List<Job>();
			for (int i = 0; i < 100; i++)
			{
				jobs.Add(new Job(
					Guid.NewGuid(),
					Constants.JobTypes.MarkAllTasksAsDone,
					JobStatus.Queued,
					DateTimeOffset.UtcNow,
					null));
			}

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = jobs.Select(j => schedulingService.ScheduleJobAsync(j));
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
				"100 concurrent job schedules should complete in under 2 seconds");

			mockScheduler.Verify(
				s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default),
				Times.Exactly(100));
		}

		[Fact]
		public async Task ConcurrentReportCreation_ShouldHandle_50ParallelCreations()
		{
			// Arrange
			var mockLogger = new Mock<ILogger<ReportService>>();
			var reportService = new ReportService(_dbContext, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = Enumerable.Range(0, 50).Select(async i =>
			{
				await reportService.CreateReportAsync(
					Guid.NewGuid(),
					$"concurrent-report-{i}.pdf",
					$"Content for report {i}",
					"application/pdf");
			});
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
				"50 parallel report creations should complete in under 2 seconds");

			var reportCount = await _dbContext.Reports.CountAsync();
			reportCount.Should().Be(50);
		}

		[Fact]
		public async Task ConcurrentRetryScheduling_ShouldHandle_30ParallelRetries()
		{
			// Arrange
			var jobs = new List<Job>();
			for (int i = 0; i < 30; i++)
			{
				jobs.Add(new Job(Guid.NewGuid(), $"Job{i}", JobStatus.Failed, DateTimeOffset.UtcNow, null));
			}
			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			var mockSchedulingService = new Mock<IJobSchedulingService>();
			var mockLogger = new Mock<ILogger<JobRetryService>>();
			var retryService = new JobRetryService(_dbContext, mockSchedulingService.Object, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			var tasks = jobs.Select(j => retryService.ScheduleRetryAsync(j.Id, $"Concurrent error for {j.Id}"));
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1),
				"30 parallel retry schedules should complete in under 1 second");
		}

		#endregion

		#region Memory Usage Tests

		[Fact]
		public async Task LargeJobCleanup_ShouldNotExceed_100MB_MemoryIncrease()
		{
			// Arrange
			var jobs = new List<Job>();
			for (int i = 0; i < 5000; i++)
			{
				jobs.Add(new Job(
					Guid.NewGuid(),
					$"Job{i}",
					JobStatus.Completed,
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
				_mockLogger.Object);

			// Force garbage collection before measurement
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var beforeMemory = GC.GetTotalMemory(false);

			// Act
			await cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var afterMemory = GC.GetTotalMemory(false);
			var memoryIncrease = afterMemory - beforeMemory;
			var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

			memoryIncreaseMB.Should().BeLessThan(100,
				$"memory increase should be less than 100MB, but was {memoryIncreaseMB:F2}MB");
		}

		[Fact]
		public async Task BulkReportCreation_ShouldNotExceed_50MB_MemoryIncrease()
		{
			// Arrange
			var mockLogger = new Mock<ILogger<ReportService>>();
			var reportService = new ReportService(_dbContext, mockLogger.Object);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var beforeMemory = GC.GetTotalMemory(false);

			// Act - Create 1000 reports
			var tasks = new List<Task<Report>>();
			for (int i = 0; i < 1000; i++)
			{
				tasks.Add(reportService.CreateReportAsync(
					Guid.NewGuid(),
					$"bulk-report-{i}.pdf",
					$"Small content for report {i}",
					"application/pdf"));
			}
			await Task.WhenAll(tasks);

			// Assert
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var afterMemory = GC.GetTotalMemory(false);
			var memoryIncrease = afterMemory - beforeMemory;
			var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

			memoryIncreaseMB.Should().BeLessThan(50,
				$"memory increase should be less than 50MB, but was {memoryIncreaseMB:F2}MB");
		}

		#endregion

		#region Database Query Performance

		[Fact]
		public async Task QueryPendingJobs_ShouldComplete_Within100ms_For10000Jobs()
		{
			// Arrange
			var jobs = new List<Job>();
			for (int i = 0; i < 10000; i++)
			{
				jobs.Add(new Job(
					Guid.NewGuid(),
					$"Job{i}",
					i % 4 == 0 ? JobStatus.Queued : JobStatus.Completed,
					DateTimeOffset.UtcNow.AddMinutes(-i),
					null));
			}
			_dbContext.Jobs.AddRange(jobs);
			await _dbContext.SaveChangesAsync();

			var stopwatch = Stopwatch.StartNew();

			// Act
			var pendingJobs = await _dbContext.Jobs
				.Where(j => j.Status == JobStatus.Queued)
				.OrderBy(j => j.ScheduledAt ?? j.CreatedAt)
				.ToListAsync();

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100),
				"querying pending jobs from 10,000 total jobs should complete in under 100ms");

			pendingJobs.Should().HaveCount(2500); // 25% are queued
		}

		[Fact]
		public async Task QueryRetryInfo_ShouldComplete_Within50ms_For5000RetryRecords()
		{
			// Arrange
			var retries = new List<JobRetryInfo>();
			for (int i = 0; i < 5000; i++)
			{
				retries.Add(new JobRetryInfo(
					Guid.NewGuid(),
					Guid.NewGuid(),
					i % 3,
					3,
					DateTimeOffset.UtcNow.AddMinutes(i),
					$"Error {i}",
					DateTimeOffset.UtcNow.AddMinutes(-i)));
			}
			_dbContext.JobRetries.AddRange(retries);
			await _dbContext.SaveChangesAsync();

			var jobId = retries[2500].JobId;
			var stopwatch = Stopwatch.StartNew();

			// Act
			var retryInfo = await _dbContext.JobRetries
				.Where(r => r.JobId == jobId)
				.FirstOrDefaultAsync();

			// Assert
			stopwatch.Stop();
			stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(50),
				"querying single retry info from 5,000 records should complete in under 50ms");

			retryInfo.Should().NotBeNull();
		}

		#endregion

		#region Throughput Tests

		[Fact]
		public async Task ReportService_ShouldProcess_AtLeast100ReportsPerSecond()
		{
			// Arrange
			var mockLogger = new Mock<ILogger<ReportService>>();
			var reportService = new ReportService(_dbContext, mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();
			const int targetReports = 200;

			// Act
			var tasks = new List<Task<Report>>();
			for (int i = 0; i < targetReports; i++)
			{
				tasks.Add(reportService.CreateReportAsync(
					Guid.NewGuid(),
					$"throughput-test-{i}.pdf",
					$"Content {i}",
					"application/pdf"));
			}
			await Task.WhenAll(tasks);

			// Assert
			stopwatch.Stop();
			var reportsPerSecond = targetReports / stopwatch.Elapsed.TotalSeconds;

			reportsPerSecond.Should().BeGreaterThan(100,
				$"service should process at least 100 reports/second, but processed {reportsPerSecond:F2}");
		}

		[Fact]
		public async Task JobCleanupService_ShouldProcess_AtLeast200JobsPerSecond()
		{
			// Arrange
			var jobs = new List<Job>();
			const int targetJobs = 500;
			for (int i = 0; i < targetJobs; i++)
			{
				jobs.Add(new Job(
					Guid.NewGuid(),
					$"Job{i}",
					JobStatus.Completed,
					DateTimeOffset.UtcNow,
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
				_mockLogger.Object);

			var stopwatch = Stopwatch.StartNew();

			// Act
			await cleanupService.CleanupAllCompletedJobsAsync();

			// Assert
			stopwatch.Stop();
			var jobsPerSecond = targetJobs / stopwatch.Elapsed.TotalSeconds;

			jobsPerSecond.Should().BeGreaterThan(200,
				$"service should process at least 200 jobs/second, but processed {jobsPerSecond:F2}");
		}

		#endregion
	}
}
