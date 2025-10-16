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
using WebBoard.API.Services.Reports;

namespace WebBoard.Tests.Jobs
{
	public class GenerateTaskListJobTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly Mock<IServiceProvider> _mockServiceProvider;
		private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
		private readonly Mock<IServiceScope> _mockScope;
		private readonly Mock<IServiceProvider> _mockScopedProvider;
		private readonly Mock<IJobCleanupService> _mockCleanupService;
		private readonly Mock<IJobStatusNotifier> _mockStatusNotifier;
		private readonly Mock<IJobRetryService> _mockRetryService;
		private readonly Mock<IReportService> _mockReportService;
		private readonly Mock<ILogger<GenerateTaskListJob>> _mockLogger;

		public GenerateTaskListJobTests()
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
			_mockReportService = new Mock<IReportService>();
			_mockLogger = new Mock<ILogger<GenerateTaskListJob>>();

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
			_mockScopedProvider.Setup(sp => sp.GetService(typeof(IReportService))).Returns(_mockReportService.Object);
		}

		public void Dispose()
		{
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
			GC.SuppressFinalize(this);
		}

		[Fact]
		public async Task Execute_WithTasks_ShouldGenerateReport()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Description 1", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Description 2", TaskItemStatus.Completed, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			var report = new Report(reportId, jobId, "report.txt", "content", "text/plain", DateTimeOffset.UtcNow);

			_mockReportService.Setup(r => r.CreateReportAsync(
				jobId,
				It.IsAny<string>(),
				It.IsAny<string>(),
				"text/plain"))
				.ReturnsAsync(report);

			_mockStatusNotifier.Setup(s => s.NotifyReportGeneratedAsync(jobId, reportId, It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var generateJob = new GenerateTaskListJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await generateJob.Execute(context);

			// Assert
			_mockReportService.Verify(r => r.CreateReportAsync(
				jobId,
				It.Is<string>(s => s.Contains("TaskList_Job_")),
				It.IsAny<string>(),
				"text/plain"), Times.Once);

			_mockStatusNotifier.Verify(s => s.NotifyReportGeneratedAsync(
				jobId,
				reportId,
				It.IsAny<string>()), Times.Once);

			var updatedJob = await _dbContext.Jobs.FindAsync(jobId);
			updatedJob!.Status.Should().Be(JobStatus.Completed);
		}

		[Fact]
		public async Task Execute_ShouldGenerateCorrectReportContent()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Important Task", "Critical description", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Done Task", "Already done", TaskItemStatus.Completed, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			string? capturedContent = null;
			_mockReportService.Setup(r => r.CreateReportAsync(
				jobId,
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>()))
				.Callback<Guid, string, string, string>((_, _, content, _) => capturedContent = content)
				.ReturnsAsync(new Report(reportId, jobId, "report.txt", "content", "text/plain", DateTimeOffset.UtcNow));

			_mockStatusNotifier.Setup(s => s.NotifyReportGeneratedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var generateJob = new GenerateTaskListJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await generateJob.Execute(context);

			// Assert
			capturedContent.Should().NotBeNull();
			capturedContent.Should().Contain("TASK LIST REPORT");
			capturedContent.Should().Contain($"Job ID: {jobId}");
			capturedContent.Should().Contain("Total Tasks: 2");
			capturedContent.Should().Contain("Important Task");
			capturedContent.Should().Contain("Critical description");
			capturedContent.Should().Contain("Done Task");
			capturedContent.Should().Contain("Already done");
			capturedContent.Should().Contain("PENDING TASKS");
			capturedContent.Should().Contain("COMPLETED TASKS");
		}

		[Fact]
		public async Task Execute_WithNoTasks_ShouldGenerateEmptyReport()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			_dbContext.Jobs.Add(job);
			await _dbContext.SaveChangesAsync();

			string? capturedContent = null;
			_mockReportService.Setup(r => r.CreateReportAsync(
				jobId,
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>()))
				.Callback<Guid, string, string, string>((_, _, content, _) => capturedContent = content)
				.ReturnsAsync(new Report(reportId, jobId, "report.txt", "content", "text/plain", DateTimeOffset.UtcNow));

			_mockStatusNotifier.Setup(s => s.NotifyReportGeneratedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var generateJob = new GenerateTaskListJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await generateJob.Execute(context);

			// Assert
			capturedContent.Should().NotBeNull();
			capturedContent.Should().Contain("Total Tasks: 0");
			capturedContent.Should().Contain("No tasks assigned to this job");

			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No tasks found")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task Execute_ShouldGroupTasksByStatus()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Pending 1", "Desc", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Pending 2", "Desc", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "In Progress", "Desc", TaskItemStatus.InProgress, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Completed 1", "Desc", TaskItemStatus.Completed, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Completed 2", "Desc", TaskItemStatus.Completed, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Completed 3", "Desc", TaskItemStatus.Completed, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			string? capturedContent = null;
			_mockReportService.Setup(r => r.CreateReportAsync(
				jobId,
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>()))
				.Callback<Guid, string, string, string>((_, _, content, _) => capturedContent = content)
				.ReturnsAsync(new Report(reportId, jobId, "report.txt", "content", "text/plain", DateTimeOffset.UtcNow));

			_mockStatusNotifier.Setup(s => s.NotifyReportGeneratedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var generateJob = new GenerateTaskListJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await generateJob.Execute(context);

			// Assert
			capturedContent.Should().NotBeNull();
			capturedContent.Should().Contain("PENDING TASKS (2):");
			capturedContent.Should().Contain("INPROGRESS TASKS (1):");
			capturedContent.Should().Contain("COMPLETED TASKS (3):");
		}

		[Fact]
		public async Task Execute_ShouldOnlyIncludeTasksFromSpecificJob()
		{
			// Arrange
			var jobId1 = Guid.NewGuid();
			var jobId2 = Guid.NewGuid();
			var reportId = Guid.NewGuid();

			var job1 = new Job(jobId1, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var job2 = new Job(jobId2, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			var tasksJob1 = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Job1 Task", "Description", TaskItemStatus.Pending, jobId1)
			};

			var tasksJob2 = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Job2 Task", "Description", TaskItemStatus.Pending, jobId2)
			};

			_dbContext.Jobs.AddRange(job1, job2);
			_dbContext.Tasks.AddRange(tasksJob1);
			_dbContext.Tasks.AddRange(tasksJob2);
			await _dbContext.SaveChangesAsync();

			string? capturedContent = null;
			_mockReportService.Setup(r => r.CreateReportAsync(
				jobId1,
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>()))
				.Callback<Guid, string, string, string>((_, _, content, _) => capturedContent = content)
				.ReturnsAsync(new Report(reportId, jobId1, "report.txt", "content", "text/plain", DateTimeOffset.UtcNow));

			_mockStatusNotifier.Setup(s => s.NotifyReportGeneratedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var generateJob = new GenerateTaskListJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId1);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId1)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId1)).Returns(Task.CompletedTask);

			// Act
			await generateJob.Execute(context);

			// Assert
			capturedContent.Should().NotBeNull();
			capturedContent.Should().Contain("Job1 Task");
			capturedContent.Should().NotContain("Job2 Task");
			capturedContent.Should().Contain("Total Tasks: 1");
		}

		[Fact]
		public async Task Execute_ShouldReportCorrectTaskCount()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var reportId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var tasks = new List<TaskItem>
			{
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Pending, jobId),
				new(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc", TaskItemStatus.Pending, jobId)
			};

			_dbContext.Jobs.Add(job);
			_dbContext.Tasks.AddRange(tasks);
			await _dbContext.SaveChangesAsync();

			_mockReportService.Setup(r => r.CreateReportAsync(
				jobId,
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>()))
				.ReturnsAsync(new Report(reportId, jobId, "report.txt", "content", "text/plain", DateTimeOffset.UtcNow));

			_mockStatusNotifier.Setup(s => s.NotifyReportGeneratedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
				.Returns(Task.CompletedTask);

			var generateJob = new GenerateTaskListJob(_mockServiceProvider.Object, _mockLogger.Object);
			var context = CreateJobExecutionContext(jobId);

			_mockRetryService.Setup(r => r.GetRetryInfoAsync(jobId)).ReturnsAsync((JobRetryInfo?)null);
			_mockRetryService.Setup(r => r.RemoveRetryInfoAsync(jobId)).Returns(Task.CompletedTask);

			// Act
			await generateJob.Execute(context);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating report for 3 tasks")),
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
