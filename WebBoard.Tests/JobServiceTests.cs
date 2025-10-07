using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Jobs;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests
{
    /// <summary>
    /// Unit tests for JobService
    /// </summary>
    public class JobServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IJobSchedulingService> _mockJobSchedulingService;
        private readonly Mock<IJobTypeRegistry> _mockJobTypeRegistry;
        private readonly JobService _jobService;

        public JobServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);

            _mockJobSchedulingService = new Mock<IJobSchedulingService>();
            _mockJobTypeRegistry = new Mock<IJobTypeRegistry>();

            // Setup default job type registry behavior
            _mockJobTypeRegistry
                .Setup(x => x.IsValidJobType(It.IsAny<string>()))
                .Returns(true);

            _mockJobTypeRegistry
                .Setup(x => x.GetAllJobTypes())
                .Returns([Constants.JobTypes.MarkAllTasksAsDone, Constants.JobTypes.GenerateTaskReport]);

            _jobService = new JobService(
                _dbContext,
                _mockJobSchedulingService.Object,
                _mockJobTypeRegistry.Object);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        #region GetJobsAsync Tests

        [Fact]
        public async Task GetJobsAsync_ShouldReturnEmptyResult_WhenNoJobsExist()
        {
            // Arrange
            var parameters = new JobQueryParameters();

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().BeEmpty();
            result.Metadata.TotalCount.Should().Be(0);
            result.Metadata.CurrentPage.Should().Be(1);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldReturnAllJobs_WhenNoFiltersApplied()
        {
            // Arrange
            var job1 = new Job(Guid.NewGuid(), "TestJob1", JobStatus.Queued, DateTimeOffset.UtcNow, null);
            var job2 = new Job(Guid.NewGuid(), "TestJob2", JobStatus.Running, DateTimeOffset.UtcNow, null);

            await _dbContext.Jobs.AddRangeAsync(job1, job2);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters();

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Metadata.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldFilterByStatus()
        {
            // Arrange
            var job1 = new Job(Guid.NewGuid(), "TestJob1", JobStatus.Queued, DateTimeOffset.UtcNow, null);
            var job2 = new Job(Guid.NewGuid(), "TestJob2", JobStatus.Running, DateTimeOffset.UtcNow, null);
            var job3 = new Job(Guid.NewGuid(), "TestJob3", JobStatus.Completed, DateTimeOffset.UtcNow, null);

            await _dbContext.Jobs.AddRangeAsync(job1, job2, job3);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters { Status = (int)JobStatus.Running };

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Status.Should().Be(JobStatus.Running);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldFilterByJobType()
        {
            // Arrange
            var job1 = new Job(Guid.NewGuid(), Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
            var job2 = new Job(Guid.NewGuid(), Constants.JobTypes.GenerateTaskReport, JobStatus.Running, DateTimeOffset.UtcNow, null);

            await _dbContext.Jobs.AddRangeAsync(job1, job2);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters { JobType = Constants.JobTypes.MarkAllTasksAsDone };

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().JobType.Should().Be(Constants.JobTypes.MarkAllTasksAsDone);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldFilterBySearchTerm()
        {
            // Arrange
            var job1 = new Job(Guid.NewGuid(), "DataProcessing", JobStatus.Queued, DateTimeOffset.UtcNow, null);
            var job2 = new Job(Guid.NewGuid(), "ReportGeneration", JobStatus.Running, DateTimeOffset.UtcNow, null);

            await _dbContext.Jobs.AddRangeAsync(job1, job2);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters { SearchTerm = "report" };

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().JobType.Should().Be("ReportGeneration");
        }

        [Fact]
        public async Task GetJobsAsync_ShouldApplyPagination()
        {
            // Arrange
            for (int i = 0; i < 15; i++)
            {
                var job = new Job(Guid.NewGuid(), $"TestJob{i}", JobStatus.Queued, DateTimeOffset.UtcNow, null);
                await _dbContext.Jobs.AddAsync(job);
            }
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters { PageNumber = 2, PageSize = 5 };

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(5);
            result.Metadata.TotalCount.Should().Be(15);
            result.Metadata.CurrentPage.Should().Be(2);
            result.Metadata.PageSize.Should().Be(5);
            result.Metadata.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetJobsAsync_ShouldIncludeRelatedReport()
        {
            // Arrange
            var job = new Job(Guid.NewGuid(), "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
            var report = new Report(Guid.NewGuid(), job.Id, "report.pdf", "Test report content", "application/pdf", DateTimeOffset.UtcNow);

            await _dbContext.Jobs.AddAsync(job);
            await _dbContext.Reports.AddAsync(report);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters();

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().HasReport.Should().BeTrue();
            result.Items.First().ReportId.Should().Be(report.Id);
            result.Items.First().ReportFileName.Should().Be("report.pdf");
        }

        [Fact]
        public async Task GetJobsAsync_ShouldIncludeAssociatedTasks()
        {
            // Arrange
            var job = new Job(Guid.NewGuid(), "TestJob", JobStatus.Running, DateTimeOffset.UtcNow, null);
            var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Description 1", TaskItemStatus.Pending, job.Id);
            var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Description 2", TaskItemStatus.Pending, job.Id);

            await _dbContext.Jobs.AddAsync(job);
            await _dbContext.Tasks.AddRangeAsync(task1, task2);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters();

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().TaskIds.Should().HaveCount(2);
            result.Items.First().TaskIds.Should().Contain([task1.Id, task2.Id]);
        }

        #endregion

        #region GetJobByIdAsync Tests

        [Fact]
        public async Task GetJobByIdAsync_ShouldReturnNull_WhenJobDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _jobService.GetJobByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetJobByIdAsync_ShouldReturnJob_WhenJobExists()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new Job(jobId, "TestJob", JobStatus.Running, DateTimeOffset.UtcNow, null);

            await _dbContext.Jobs.AddAsync(job);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobByIdAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(jobId);
            result.JobType.Should().Be("TestJob");
            result.Status.Should().Be(JobStatus.Running);
        }

        [Fact]
        public async Task GetJobByIdAsync_ShouldIncludeReport_WhenReportExists()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new Job(jobId, "TestJob", JobStatus.Completed, DateTimeOffset.UtcNow, null);
            var report = new Report(Guid.NewGuid(), jobId, "report.pdf", "Content", "application/pdf", DateTimeOffset.UtcNow);

            await _dbContext.Jobs.AddAsync(job);
            await _dbContext.Reports.AddAsync(report);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobByIdAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result!.HasReport.Should().BeTrue();
            result.ReportId.Should().Be(report.Id);
            result.ReportFileName.Should().Be("report.pdf");
        }

        [Fact]
        public async Task GetJobByIdAsync_ShouldIncludeTasks_WhenTasksExist()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new Job(jobId, "TestJob", JobStatus.Running, DateTimeOffset.UtcNow, null);
            var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, jobId);
            var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Pending, jobId);

            await _dbContext.Jobs.AddAsync(job);
            await _dbContext.Tasks.AddRangeAsync(task1, task2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobByIdAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result!.TaskIds.Should().HaveCount(2);
            result.TaskIds.Should().Contain([task1.Id, task2.Id]);
        }

        #endregion

        #region CreateJobAsync Tests

        [Fact]
        public async Task CreateJobAsync_ShouldThrowException_WhenJobTypeIsInvalid()
        {
            // Arrange
            _mockJobTypeRegistry.Setup(x => x.IsValidJobType("InvalidJobType")).Returns(false);

            var request = new CreateJobRequestDto("InvalidJobType", true, null, [Guid.NewGuid()]);

            // Act
            Func<Task> act = () => _jobService.CreateJobAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid job type*");
        }

        [Fact]
        public async Task CreateJobAsync_ShouldThrowException_WhenNoTasksSelected()
        {
            // Arrange
            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, []);

            // Act
            Func<Task> act = () => _jobService.CreateJobAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("At least one task must be selected*");
        }

        [Fact]
        public async Task CreateJobAsync_ShouldThrowException_WhenTasksDoNotExist()
        {
            // Arrange
            var nonExistentTaskId = Guid.NewGuid();
			var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [nonExistentTaskId]);

            // Act
            Func<Task> act = () => _jobService.CreateJobAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*do not exist*");
        }

        [Fact]
        public async Task CreateJobAsync_ShouldThrowException_WhenScheduledTimeIsInPast()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var pastTime = DateTimeOffset.UtcNow.AddHours(-1);
            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, false, pastTime, [task.Id]);

            // Act
            Func<Task> act = () => _jobService.CreateJobAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Scheduled time cannot be in the past*");
        }

        [Fact]
        public async Task CreateJobAsync_ShouldThrowException_WhenTasksAreNotPending_ForMarkAllTasksAsDone()
        {
            // Arrange
            var completedTask = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Completed, null);
            await _dbContext.Tasks.AddAsync(completedTask);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [completedTask.Id]);

            // Act
            Func<Task> act = () => _jobService.CreateJobAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*can only process pending tasks*");
        }

        [Fact]
        public async Task CreateJobAsync_ShouldThrowException_WhenTasksAlreadyAssignedToJob()
        {
            // Arrange
            var existingJobId = Guid.NewGuid();
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, existingJobId);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task.Id]);

            // Act
            Func<Task> act = () => _jobService.CreateJobAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*already assigned to another job*");
        }

        [Fact]
        public async Task CreateJobAsync_ShouldCreateJob_WhenAllValidationsPass()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task.Id]);

            // Act
            var result = await _jobService.CreateJobAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.JobType.Should().Be(Constants.JobTypes.MarkAllTasksAsDone);
            result.Status.Should().Be(JobStatus.Queued);
            result.TaskIds.Should().Contain(task.Id);
        }

        [Fact]
        public async Task CreateJobAsync_ShouldAssignTasksToJob()
        {
            // Arrange
            var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, null);
            var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddRangeAsync(task1, task2);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task1.Id, task2.Id]);

            // Act
            var result = await _jobService.CreateJobAsync(request);

            // Assert
            var updatedTasks = await _dbContext.Tasks
                .Where(t => t.Id == task1.Id || t.Id == task2.Id)
                .ToListAsync();

            updatedTasks.Should().HaveCount(2);
            updatedTasks.Should().OnlyContain(t => t.JobId == result.Id);
        }

        [Fact]
        public async Task CreateJobAsync_ShouldScheduleJob()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task.Id]);

            // Act
            await _jobService.CreateJobAsync(request);

            // Assert
            _mockJobSchedulingService.Verify(
                x => x.ScheduleJobAsync(It.IsAny<Job>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateJobAsync_ShouldSetScheduledAt_WhenNotRunImmediately()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var scheduledTime = DateTimeOffset.UtcNow.AddHours(2);
            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, false, scheduledTime, [task.Id]);

            // Act
            var result = await _jobService.CreateJobAsync(request);

            // Assert
            result.ScheduledAt.Should().Be(scheduledTime);
        }

        [Fact]
        public async Task CreateJobAsync_ShouldSetScheduledAtToNull_WhenRunImmediately()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task.Id]);

            // Act
            var result = await _jobService.CreateJobAsync(request);

            // Assert
            result.ScheduledAt.Should().BeNull();
        }

        [Fact]
        public async Task CreateJobAsync_ShouldAllowGenerateTaskReport_WithAnyTaskStatus()
        {
            // Arrange
            var completedTask = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Completed, null);
            await _dbContext.Tasks.AddAsync(completedTask);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.GenerateTaskReport, true, null, [completedTask.Id]);

            // Act
            var result = await _jobService.CreateJobAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.JobType.Should().Be(Constants.JobTypes.GenerateTaskReport);
        }

        [Fact]
        public async Task CreateJobAsync_ShouldPersistJobToDatabase()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task.Id]);

            // Act
            var result = await _jobService.CreateJobAsync(request);

            // Assert
            var savedJob = await _dbContext.Jobs.FindAsync(result.Id);
            savedJob.Should().NotBeNull();
            savedJob!.JobType.Should().Be(Constants.JobTypes.MarkAllTasksAsDone);
            savedJob.Status.Should().Be(JobStatus.Queued);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CreateAndRetrieveJob_ShouldWorkCorrectly()
        {
            // Arrange
            var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            var createRequest = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, [task.Id]);

            // Act
            var createdJob = await _jobService.CreateJobAsync(createRequest);
            var retrievedJob = await _jobService.GetJobByIdAsync(createdJob.Id);

            // Assert
            retrievedJob.Should().NotBeNull();
            retrievedJob!.Id.Should().Be(createdJob.Id);
            retrievedJob.JobType.Should().Be(createdJob.JobType);
            retrievedJob.Status.Should().Be(createdJob.Status);
            retrievedJob.TaskIds.Should().BeEquivalentTo(createdJob.TaskIds);
        }

        [Fact]
        public async Task GetJobsAsync_WithMultipleFilters_ShouldApplyAllFilters()
        {
            // Arrange
            var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, null);
            await _dbContext.Tasks.AddAsync(task1);

            var job1 = new Job(Guid.NewGuid(), "DataProcessing", JobStatus.Running, DateTimeOffset.UtcNow, null);
            var job2 = new Job(Guid.NewGuid(), "DataProcessing", JobStatus.Completed, DateTimeOffset.UtcNow, null);
            var job3 = new Job(Guid.NewGuid(), "ReportGeneration", JobStatus.Running, DateTimeOffset.UtcNow, null);

            await _dbContext.Jobs.AddRangeAsync(job1, job2, job3);
            await _dbContext.SaveChangesAsync();

            var parameters = new JobQueryParameters
            {
                Status = (int)JobStatus.Running,
                JobType = "DataProcessing",
                PageSize = 10
            };

            // Act
            var result = await _jobService.GetJobsAsync(parameters);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Status.Should().Be(JobStatus.Running);
            result.Items.First().JobType.Should().Be("DataProcessing");
        }

        #endregion
    }
}
