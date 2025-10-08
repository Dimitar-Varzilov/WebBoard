using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests.Jobs
{
	public class MarkTasksAsCompletedJobTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IServiceProvider> _mockServiceProvider;
		private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
		private readonly Mock<IServiceScope> _mockScope;
		private readonly Mock<IServiceProvider> _mockScopedProvider;
		private readonly Mock<IJobCleanupService> _mockCleanupService;
		private readonly Mock<IJobStatusNotifier> _mockStatusNotifier;
		private readonly Mock<IJobRetryService> _mockRetryService;
		private readonly Mock<ILogger<MarkTasksAsCompletedJob>> _mockLogger;

		public MarkTasksAsCompletedJobTests()
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
			_mockLogger = new Mock<ILogger<MarkTasksAsCompletedJob>>();

			// Setup service provider chain
			_mockServiceProvider = new Mock<IServiceProvider>();
			_mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
				.Returns(_mockScopeFactory.Object);

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
		public async Task Execute_WithPendingTasks_ShouldMarkThemAsCompleted()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Description 1", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Description 2", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Description 3", TaskItemStatus.Pending, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			var markTasksJob = new MarkTasksAsCompletedJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await markTasksJob.Execute(context);

			// Assert
			var updatedTasks = await _dbContext.Tasks.Where(t => t.JobId == jobId).ToListAsync();
			updatedTasks.Should().HaveCount(3);
			updatedTasks.Should().OnlyContain(t => t.Status == TaskItemStatus.Completed);

			var updatedJob = await _dbContext.Jobs.FindAsync(jobId);
			updatedJob!.Status.Should().Be(JobStatus.Completed);
		}

		[Fact]
		public async Task Execute_WithNoPendingTasks_ShouldStillComplete()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Description 1", TaskItemStatus.Completed, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Description 2", TaskItemStatus.Completed, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			var markTasksJob = new MarkTasksAsCompletedJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await markTasksJob.Execute(context);

			// Assert
			var updatedJob = await _dbContext.Jobs.FindAsync(jobId);
			updatedJob!.Status.Should().Be(JobStatus.Completed);

			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No pending tasks found")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task Execute_ShouldOnlyUpdateTasksForSpecificJob()
		{
			// Arrange
			var jobId1 = Guid.NewGuid();
			var jobId2 = Guid.NewGuid();
			
			var job1 = new Job(jobId1, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var job2 = new Job(jobId2, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			
			var tasksJob1 = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Job1 Task 1", "Description", TaskItemStatus.Pending, jobId1),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Job1 Task 2", "Description", TaskItemStatus.Pending, jobId1)
			};

			var tasksJob2 = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Job2 Task 1", "Description", TaskItemStatus.Pending, jobId2)
			};

			_dbContext.Jobs.AddRange(job1, job2);
			_dbContext.Tasks.AddRange(tasksJob1);
			_dbContext.Tasks.AddRange(tasksJob2);
			await _dbContext.SaveChangesAsync();

			var markTasksJob = new MarkTasksAsCompletedJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId1);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId1)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId1)).Returns(Task.CompletedTask);

			// Act
			await markTasksJob.Execute(context);

			// Assert
			var job1Tasks = await _dbContext.Tasks.Where(t => t.JobId == jobId1).ToListAsync();
			job1Tasks.Should().OnlyContain(t => t.Status == TaskItemStatus.Completed);

			var job2Tasks = await _dbContext.Tasks.Where(t => t.JobId == jobId2).ToListAsync();
			job2Tasks.Should().OnlyContain(t => t.Status == TaskItemStatus.Pending);
		}

		[Fact]
		public async Task Execute_WithMixedStatusTasks_ShouldOnlyUpdatePending()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Description", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Description", TaskItemStatus.InProgress, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Description", TaskItemStatus.Completed, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			var markTasksJob = new MarkTasksAsCompletedJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await markTasksJob.Execute(context);

			// Assert
			var updatedTasks = await _dbContext.Tasks.Where(t => t.JobId == jobId).ToListAsync();
			
			// Only 1 pending task should be updated by the job logic
			// The other tasks (InProgress, Completed) are updated by BaseJob's UpdateJobTasksOnCompletionAsync
			updatedTasks.Where(t => t.Status == TaskItemStatus.Completed).Should().HaveCount(3);
		}

		[Fact]
		public async Task Execute_ShouldReportCorrectTaskCount()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Description", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Description", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Description", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 4", "Description", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 5", "Description", TaskItemStatus.Pending, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			var markTasksJob = new MarkTasksAsCompletedJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await markTasksJob.Execute(context);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Marked 5 tasks")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task Execute_WithNoJobId_ShouldNotCrash()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var markTasksJob = new MarkTasksAsCompletedJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			// Act
			await markTasksJob.Execute(context);

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

		private IJobExecutionContext CreateJobExecutionContext(Guid jobId)
		{
			var mockContext = new Mock<IJobExecutionContext>();
			var jobDataMap = new JobDataMap();
			jobDataMap.Put("JobId", jobId);

			mockContext.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
			mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

			return mockContext.Object;
		}
	}
}
