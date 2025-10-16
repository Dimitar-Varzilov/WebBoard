using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebBoard.API.Common.Constants;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Jobs;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Controllers;
using WebBoard.API.Services.Jobs;
using WebBoard.API.Services.Tasks;

namespace WebBoard.Tests.Controllers
{
	public class JobsControllerTests
	{
		private readonly Mock<IJobService> _mockJobService;
		private readonly Mock<ITaskService> _mockTaskService;
		private readonly JobsController _controller;

		public JobsControllerTests()
		{
			_mockJobService = new Mock<IJobService>();
			_mockTaskService = new Mock<ITaskService>();
			_controller = new JobsController(_mockJobService.Object, _mockTaskService.Object);
		}

		#region GetJobs Tests

		[Fact]
		public async Task GetJobs_ShouldReturnOkWithPagedResult()
		{
			// Arrange
			var parameters = new JobQueryParameters { Page = 1, PageSize = 10 };
			var expectedResult = new PagedResult<JobDto>(
				[
					new(Guid.NewGuid(), Constants.JobTypes.MarkAllTasksAsDone, JobStatus.Completed, DateTimeOffset.UtcNow, null)
				],
				1, 1, 10
			);

			_mockJobService.Setup(s => s.GetJobsAsync(parameters))
				.ReturnsAsync(expectedResult);

			// Act
			var result = await _controller.GetJobs(parameters);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(expectedResult);
			_mockJobService.Verify(s => s.GetJobsAsync(parameters), Times.Once);
		}

		[Fact]
		public async Task GetJobs_ShouldApplyFiltersCorrectly()
		{
			// Arrange
			var parameters = new JobQueryParameters
			{
				Status = (int)JobStatus.Running,
				Page = 1,
				PageSize = 20
			};

			var expectedResult = new PagedResult<JobDto>(
				[],
				0, 1, 20
			);

			_mockJobService.Setup(s => s.GetJobsAsync(parameters))
				.ReturnsAsync(expectedResult);

			// Act
			var result = await _controller.GetJobs(parameters);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			_mockJobService.Verify(s => s.GetJobsAsync(parameters), Times.Once);
		}

		#endregion

		#region GetJobById Tests

		[Fact]
		public async Task GetJobById_WhenJobExists_ShouldReturnOk()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var expectedJob = new JobDto(jobId, Constants.JobTypes.GenerateTaskReport, JobStatus.Completed, DateTimeOffset.UtcNow, null);

			_mockJobService.Setup(s => s.GetJobByIdAsync(jobId))
				.ReturnsAsync(expectedJob);

			// Act
			var result = await _controller.GetJobById(jobId);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(expectedJob);
		}

		[Fact]
		public async Task GetJobById_WhenJobDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.GetJobByIdAsync(jobId))
				.ReturnsAsync((JobDto?)null);

			// Act
			var result = await _controller.GetJobById(jobId);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		#endregion

		#region GetPendingTasksCount Tests

		[Fact]
		public async Task GetPendingTasksCount_ShouldReturnCorrectCount()
		{
			// Arrange
			const int expectedCount = 42;
			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(TaskItemStatus.Pending))
				.ReturnsAsync(expectedCount);

			// Act
			var result = await _controller.GetPendingTasksCount();

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().Be(expectedCount);
		}

		[Fact]
		public async Task GetPendingTasksCount_WhenNoTasks_ShouldReturnZero()
		{
			// Arrange
			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(TaskItemStatus.Pending))
				.ReturnsAsync(0);

			// Act
			var result = await _controller.GetPendingTasksCount();

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().Be(0);
		}

		#endregion

		#region GetAvailableTasksForJob Tests

		[Fact]
		public async Task GetAvailableTasksForJob_WithValidJobType_ShouldReturnOk()
		{
			// Arrange
			var jobType = Constants.JobTypes.MarkAllTasksAsDone;
			var expectedTasks = new List<TaskDto>
			{
				new(Guid.NewGuid(), "Task 1", "Description 1", TaskItemStatus.Pending, DateTimeOffset.UtcNow),
				new(Guid.NewGuid(), "Task 2", "Description 2", TaskItemStatus.Pending, DateTimeOffset.UtcNow)
			};

			_mockTaskService.Setup(s => s.GetTasksByStatusAsync(TaskItemStatus.Pending))
				.ReturnsAsync(expectedTasks);

			// Act
			var result = await _controller.GetAvailableTasksForJob(jobType);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var tasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskDto>>().Subject;
			tasks.Should().HaveCount(2);
		}

		[Fact]
		public async Task GetAvailableTasksForJob_WithEmptyJobType_ShouldReturnBadRequest()
		{
			// Act
			var result = await _controller.GetAvailableTasksForJob("");

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task GetAvailableTasksForJob_WithNullJobType_ShouldReturnBadRequest()
		{
			// Act
			var result = await _controller.GetAvailableTasksForJob(null!);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task GetAvailableTasksForJob_ForMarkAllTasksAsDone_ShouldFilterPendingTasksWithoutJob()
		{
			// Arrange
			var jobType = Constants.JobTypes.MarkAllTasksAsDone;
			var tasks = new List<TaskDto>
			{
				new(Guid.NewGuid(), "Task 1", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow, null),
				new(Guid.NewGuid(), "Task 2", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow, Guid.NewGuid())
			};

			_mockTaskService.Setup(s => s.GetTasksByStatusAsync(TaskItemStatus.Pending))
				.ReturnsAsync(tasks);

			// Act
			var result = await _controller.GetAvailableTasksForJob(jobType);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var availableTasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskDto>>().Subject;
			availableTasks.Should().HaveCount(1); // Only task without JobId
		}

		[Fact]
		public async Task GetAvailableTasksForJob_ForOtherJobTypes_ShouldUsePagedQuery()
		{
			// Arrange
			var jobType = Constants.JobTypes.GenerateTaskReport;
			var pagedResult = new PagedResult<TaskDto>(
				[
					new(Guid.NewGuid(), "Task", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow)
				],
				1, 1, 1000
			);

			_mockTaskService.Setup(s => s.GetTasksAsync(It.IsAny<TaskQueryParameters>()))
				.ReturnsAsync(pagedResult);

			// Act
			var result = await _controller.GetAvailableTasksForJob(jobType);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			_mockTaskService.Verify(s => s.GetTasksAsync(It.IsAny<TaskQueryParameters>()), Times.Once);
		}

		#endregion

		#region CreateJob Tests

		[Fact]
		public async Task CreateJob_WithValidRequest_ShouldReturnCreatedAtAction()
		{
			// Arrange
			var request = new CreateJobRequestDto(
				Constants.JobTypes.MarkAllTasksAsDone,
				true,
				null,
				[Guid.NewGuid()]);

			var createdJob = new JobDto(
				Guid.NewGuid(),
				Constants.JobTypes.MarkAllTasksAsDone,
				JobStatus.Queued,
				DateTimeOffset.UtcNow,
				null);

			_mockJobService.Setup(s => s.CreateJobAsync(request))
				.ReturnsAsync(createdJob);

			// Act
			var result = await _controller.CreateJob(request);

			// Assert
			var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
			createdResult.ActionName.Should().Be(nameof(JobsController.GetJobById));
			createdResult.RouteValues!["id"].Should().Be(createdJob.Id);
			createdResult.Value.Should().BeEquivalentTo(createdJob);
		}

		[Fact]
		public async Task CreateJob_WithInvalidJobType_ShouldReturnBadRequest()
		{
			// Arrange
			var request = new CreateJobRequestDto("InvalidJobType", true, null, [Guid.NewGuid()]);
			_mockJobService.Setup(s => s.CreateJobAsync(request))
				.ThrowsAsync(new ArgumentException("Invalid job type"));

			// Act
			var result = await _controller.CreateJob(request);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task CreateJob_WithNoPendingTasks_ShouldReturnBadRequest()
		{
			// Arrange
			var request = new CreateJobRequestDto(Constants.JobTypes.MarkAllTasksAsDone, true, null, []);
			_mockJobService.Setup(s => s.CreateJobAsync(request))
				.ThrowsAsync(new InvalidOperationException("No pending tasks available"));

			// Act
			var result = await _controller.CreateJob(request);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task CreateJob_WithScheduledTime_ShouldCreateScheduledJob()
		{
			// Arrange
			var scheduledTime = DateTimeOffset.UtcNow.AddHours(1);
			var request = new CreateJobRequestDto(
				Constants.JobTypes.GenerateTaskReport,
				false,
				scheduledTime,
				[Guid.NewGuid()]);

			var createdJob = new JobDto(
				Guid.NewGuid(),
				Constants.JobTypes.GenerateTaskReport,
				JobStatus.Queued,
				DateTimeOffset.UtcNow,
				scheduledTime);

			_mockJobService.Setup(s => s.CreateJobAsync(request))
				.ReturnsAsync(createdJob);

			// Act
			var result = await _controller.CreateJob(request);

			// Assert
			var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
			var returnedJob = createdResult.Value.Should().BeAssignableTo<JobDto>().Subject;
			returnedJob.ScheduledAt.Should().Be(scheduledTime);
		}

		#endregion

		#region UpdateJob Tests

		[Fact]
		public async Task UpdateJob_WithValidRequest_ShouldReturnOk()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.GenerateTaskReport,
				true,
				null,
				[Guid.NewGuid()]);

			var updatedJob = new JobDto(
				jobId,
				Constants.JobTypes.GenerateTaskReport,
				JobStatus.Queued,
				DateTimeOffset.UtcNow,
				null);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ReturnsAsync(updatedJob);

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(updatedJob);
		}

		[Fact]
		public async Task UpdateJob_WhenJobDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.MarkAllTasksAsDone,
				true,
				null,
				[Guid.NewGuid()]);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ReturnsAsync((JobDto?)null);

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task UpdateJob_WhenJobIsNotQueued_ShouldReturnConflict()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.MarkAllTasksAsDone,
				true,
				null,
				[Guid.NewGuid()]);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ThrowsAsync(new InvalidOperationException("Cannot update a running job. Only queued jobs can be edited."));

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
			conflictResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task UpdateJob_WhenJobIsRunning_ShouldReturnConflict()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.MarkAllTasksAsDone,
				true,
				null,
				[Guid.NewGuid()]);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ThrowsAsync(new InvalidOperationException("Cannot update a running job"));

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			result.Should().BeOfType<ConflictObjectResult>();
		}

		[Fact]
		public async Task UpdateJob_WhenJobIsCompleted_ShouldReturnConflict()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.MarkAllTasksAsDone,
				true,
				null,
				[Guid.NewGuid()]);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ThrowsAsync(new InvalidOperationException("Cannot update a completed job"));

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			result.Should().BeOfType<ConflictObjectResult>();
		}

		[Fact]
		public async Task UpdateJob_WithInvalidJobType_ShouldReturnBadRequest()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				"InvalidJobType",
				true,
				null,
				[Guid.NewGuid()]);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ThrowsAsync(new ArgumentException("Invalid job type"));

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task UpdateJob_WithNoTasksSelected_ShouldReturnBadRequest()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.MarkAllTasksAsDone,
				true,
				null,
				[]);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ThrowsAsync(new ArgumentException("At least one task must be selected"));

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task UpdateJob_WithScheduledTime_ShouldUpdateScheduledAt()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var scheduledTime = DateTimeOffset.UtcNow.AddHours(2);
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.GenerateTaskReport,
				false,
				scheduledTime,
				[Guid.NewGuid()]);

			var updatedJob = new JobDto(
				jobId,
				Constants.JobTypes.GenerateTaskReport,
				JobStatus.Queued,
				DateTimeOffset.UtcNow,
				scheduledTime);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ReturnsAsync(updatedJob);

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var returnedJob = okResult.Value.Should().BeAssignableTo<JobDto>().Subject;
			returnedJob.ScheduledAt.Should().Be(scheduledTime);
		}

		[Fact]
		public async Task UpdateJob_ChangingJobType_ShouldReturnUpdatedJob()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var request = new UpdateJobRequestDto(
				Constants.JobTypes.GenerateTaskReport,
				true,
				null,
				[Guid.NewGuid()]);

			var updatedJob = new JobDto(
				jobId,
				Constants.JobTypes.GenerateTaskReport,
				JobStatus.Queued,
				DateTimeOffset.UtcNow,
				null);

			_mockJobService.Setup(s => s.UpdateJobAsync(jobId, request))
				.ReturnsAsync(updatedJob);

			// Act
			var result = await _controller.UpdateJob(jobId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var returnedJob = okResult.Value.Should().BeAssignableTo<JobDto>().Subject;
			returnedJob.JobType.Should().Be(Constants.JobTypes.GenerateTaskReport);
		}

		#endregion

		#region DeleteJob Tests

		[Fact]
		public async Task DeleteJob_WithValidQueuedJob_ShouldReturnNoContent()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.DeleteJobAsync(jobId))
				.ReturnsAsync(true);

			// Act
			var result = await _controller.DeleteJob(jobId);

			// Assert
			result.Should().BeOfType<NoContentResult>();
		}

		[Fact]
		public async Task DeleteJob_WhenJobDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.DeleteJobAsync(jobId))
				.ReturnsAsync(false);

			// Act
			var result = await _controller.DeleteJob(jobId);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task DeleteJob_WhenJobIsRunning_ShouldReturnConflict()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.DeleteJobAsync(jobId))
				.ThrowsAsync(new InvalidOperationException("Cannot delete a running job. Only queued jobs can be deleted."));

			// Act
			var result = await _controller.DeleteJob(jobId);

			// Assert
			var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
			conflictResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task DeleteJob_WhenJobIsCompleted_ShouldReturnConflict()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.DeleteJobAsync(jobId))
				.ThrowsAsync(new InvalidOperationException("Cannot delete a completed job. Only queued jobs can be deleted."));

			// Act
			var result = await _controller.DeleteJob(jobId);

			// Assert
			result.Should().BeOfType<ConflictObjectResult>();
		}

		[Fact]
		public async Task DeleteJob_WhenJobIsFailed_ShouldReturnConflict()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.DeleteJobAsync(jobId))
				.ThrowsAsync(new InvalidOperationException("Cannot delete a failed job"));

			// Act
			var result = await _controller.DeleteJob(jobId);

			// Assert
			result.Should().BeOfType<ConflictObjectResult>();
		}

		[Fact]
		public async Task DeleteJob_ShouldCallDeleteJobAsync()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			_mockJobService.Setup(s => s.DeleteJobAsync(jobId))
				.ReturnsAsync(true);

			// Act
			await _controller.DeleteJob(jobId);

			// Assert
			_mockJobService.Verify(s => s.DeleteJobAsync(jobId), Times.Once);
		}

		#endregion
	}
}
