using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests
{
	public class JobSchedulingServiceTests
	{
		private readonly Mock<IScheduler> _mockScheduler;
		private readonly Mock<IJobTypeRegistry> _mockJobTypeRegistry;
		private readonly Mock<ILogger<JobSchedulingService>> _mockLogger;
		private readonly JobSchedulingService _schedulingService;

		public JobSchedulingServiceTests()
		{
			_mockScheduler = new Mock<IScheduler>();
			_mockJobTypeRegistry = new Mock<IJobTypeRegistry>();
			_mockLogger = new Mock<ILogger<JobSchedulingService>>();

			_schedulingService = new JobSchedulingService(
				_mockScheduler.Object,
				_mockJobTypeRegistry.Object,
				_mockLogger.Object);
		}

		#region ScheduleJobAsync Tests

		[Fact]
		public async Task ScheduleJobAsync_ShouldScheduleJob_WhenJobDoesNotExist()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var jobKey = new JobKey(jobId.ToString());

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockScheduler.Verify(s => s.CheckExists(It.Is<JobKey>(k => k.Name == jobId.ToString()), default), Times.Once);
			_mockScheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default), Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldDeleteAndReschedule_WhenJobAlreadyExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var jobKey = new JobKey(jobId.ToString());

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockScheduler.Verify(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == jobId.ToString()), default), Times.Once);
			_mockScheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default), Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldLogInformation_WhenJobAlreadyExists()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockScheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), default)).ReturnsAsync(true);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Job {jobId} already exists in scheduler")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldScheduleImmediately_WhenScheduledAtIsNull()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockScheduler.Verify(
				s => s.ScheduleJob(
					It.IsAny<IJobDetail>(),
					It.Is<ITrigger>(t => t.StartTimeUtc <= DateTimeOffset.UtcNow.AddSeconds(5)),
					default),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldScheduleAtSpecificTime_WhenScheduledAtIsFuture()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var scheduledAt = DateTimeOffset.UtcNow.AddHours(1);
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, scheduledAt);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockScheduler.Verify(
				s => s.ScheduleJob(
					It.IsAny<IJobDetail>(),
					It.Is<ITrigger>(t => t.StartTimeUtc >= scheduledAt.AddSeconds(-1) && t.StartTimeUtc <= scheduledAt.AddSeconds(1)),
					default),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldScheduleImmediately_WhenScheduledAtIsInPast()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var scheduledAt = DateTimeOffset.UtcNow.AddHours(-1);
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, scheduledAt);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockScheduler.Verify(
				s => s.ScheduleJob(
					It.IsAny<IJobDetail>(),
					It.Is<ITrigger>(t => t.StartTimeUtc <= DateTimeOffset.UtcNow.AddSeconds(5)),
					default),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldLogWarning_WhenScheduledAtIsInPast()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var scheduledAt = DateTimeOffset.UtcNow.AddHours(-1);
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, scheduledAt);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Job {jobId} scheduled time") &&
						v.ToString()!.Contains("is in the past")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldLogInformation_AfterScheduling()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Scheduled job {jobId}") &&
						v.ToString()!.Contains(Constants.JobTypes.MarkAllTasksAsDone)),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldThrowAndLogError_WhenExceptionOccurs()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var expectedException = new InvalidOperationException("Scheduler error");

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default))
				.ThrowsAsync(expectedException);

			// Act
			Func<Task> act = () => _schedulingService.ScheduleJobAsync(job);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Scheduler error");

			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Error,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) =>
						v.ToString()!.Contains($"Error scheduling job {jobId}")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldUseCorrectJobType_FromRegistry()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var jobType = Constants.JobTypes.GenerateTaskReport;
			var job = new Job(jobId, jobType, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(jobType))
				.Returns(typeof(GenerateTaskListJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockJobTypeRegistry.Verify(r => r.GetJobType(jobType), Times.Once);
			_mockScheduler.Verify(
				s => s.ScheduleJob(
					It.Is<IJobDetail>(d => d.JobType == typeof(GenerateTaskListJob)),
					It.IsAny<ITrigger>(),
					default),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleJobAsync_ShouldIncludeJobIdInJobDataMap()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var job = new Job(jobId, Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job);

			// Assert
			_mockScheduler.Verify(
				s => s.ScheduleJob(
					It.Is<IJobDetail>(d => 
						d.JobDataMap.ContainsKey(Constants.JobDataKeys.JobId) &&
						(Guid)d.JobDataMap[Constants.JobDataKeys.JobId] == jobId),
					It.IsAny<ITrigger>(),
					default),
				Times.Once);
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task ScheduleJobAsync_ShouldWorkForDifferentJobTypes()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), Constants.JobTypes.GenerateTaskReport, JobStatus.Queued, DateTimeOffset.UtcNow, null);

			_mockScheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), default)).ReturnsAsync(false);
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone))
				.Returns(typeof(MarkTasksAsCompletedJob));
			_mockJobTypeRegistry.Setup(r => r.GetJobType(Constants.JobTypes.GenerateTaskReport))
				.Returns(typeof(GenerateTaskListJob));

			// Act
			await _schedulingService.ScheduleJobAsync(job1);
			await _schedulingService.ScheduleJobAsync(job2);

			// Assert
			_mockScheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), default), Times.Exactly(2));
			_mockJobTypeRegistry.Verify(r => r.GetJobType(Constants.JobTypes.MarkAllTasksAsDone), Times.Once);
			_mockJobTypeRegistry.Verify(r => r.GetJobType(Constants.JobTypes.GenerateTaskReport), Times.Once);
		}

		#endregion
	}
}
