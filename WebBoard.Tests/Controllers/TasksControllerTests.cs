using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Controllers;
using WebBoard.API.Services.Tasks;

namespace WebBoard.Tests.Controllers
{
	public class TasksControllerTests
	{
		private readonly Mock<ITaskService> _mockTaskService;
		private readonly TasksController _controller;

		public TasksControllerTests()
		{
			_mockTaskService = new Mock<ITaskService>();
			_controller = new TasksController(_mockTaskService.Object);
		}

		#region GetTasks Tests

		[Fact]
		public async Task GetTasks_ShouldReturnOkWithPagedResult()
		{
			// Arrange
			var parameters = new TaskQueryParameters { PageNumber = 1, PageSize = 10 };
			var expectedResult = new PagedResult<TaskDto>(
				new List<TaskDto>
				{
					new(Guid.NewGuid(), "Task 1", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow, null)
				},
				1, 1, 10
			);

			_mockTaskService.Setup(s => s.GetTasksAsync(parameters))
				.ReturnsAsync(expectedResult);

			// Act
			var result = await _controller.GetTasks(parameters);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(expectedResult);
		}

		[Fact]
		public async Task GetTasks_WithFilters_ShouldApplyCorrectly()
		{
			// Arrange
			var parameters = new TaskQueryParameters 
			{ 
				Status = (int)TaskItemStatus.Completed,
				HasJob = true,
				PageNumber = 1,
				PageSize = 20
			};

			var expectedResult = new PagedResult<TaskDto>(
				new List<TaskDto>(),
				0, 1, 20
			);

			_mockTaskService.Setup(s => s.GetTasksAsync(parameters))
				.ReturnsAsync(expectedResult);

			// Act
			var result = await _controller.GetTasks(parameters);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			_mockTaskService.Verify(s => s.GetTasksAsync(parameters), Times.Once);
		}

		#endregion

		#region GetTasksByStatus Tests

		[Fact]
		public async Task GetTasksByStatus_WithValidStatus_ShouldReturnOk()
		{
			// Arrange
			var status = TaskItemStatus.Pending;
			var expectedTasks = new List<TaskDto>
			{
				new(Guid.NewGuid(), "Task 1", "Description 1", TaskItemStatus.Pending, DateTimeOffset.UtcNow, null),
				new(Guid.NewGuid(), "Task 2", "Description 2", TaskItemStatus.Pending, DateTimeOffset.UtcNow, null)
			};

			_mockTaskService.Setup(s => s.GetTasksByStatusAsync(status))
				.ReturnsAsync(expectedTasks);

			// Act
			var result = await _controller.GetTasksByStatus(status);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var tasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskDto>>().Subject;
			tasks.Should().HaveCount(2);
		}

		[Theory]
		[InlineData(TaskItemStatus.Pending)]
		[InlineData(TaskItemStatus.InProgress)]
		[InlineData(TaskItemStatus.Completed)]
		public async Task GetTasksByStatus_WithDifferentStatuses_ShouldFilterCorrectly(TaskItemStatus status)
		{
			// Arrange
			var tasks = new List<TaskDto>
			{
				new(Guid.NewGuid(), "Task", "Description", status, DateTimeOffset.UtcNow, null)
			};

			_mockTaskService.Setup(s => s.GetTasksByStatusAsync(status))
				.ReturnsAsync(tasks);

			// Act
			var result = await _controller.GetTasksByStatus(status);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			_mockTaskService.Verify(s => s.GetTasksByStatusAsync(status), Times.Once);
		}

		#endregion

		#region GetTaskCountByStatus Tests

		[Fact]
		public async Task GetTaskCountByStatus_WithValidStatus_ShouldReturnCount()
		{
			// Arrange
			var status = TaskItemStatus.Pending;
			const int expectedCount = 15;

			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(status))
				.ReturnsAsync(expectedCount);

			// Act
			var result = await _controller.GetTaskCountByStatus(status);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().Be(expectedCount);
		}

		[Fact]
		public async Task GetTaskCountByStatus_WhenNoTasks_ShouldReturnZero()
		{
			// Arrange
			var status = TaskItemStatus.Completed;
			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(status))
				.ReturnsAsync(0);

			// Act
			var result = await _controller.GetTaskCountByStatus(status);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().Be(0);
		}

		#endregion

		#region GetTaskById Tests

		[Fact]
		public async Task GetTaskById_WhenTaskExists_ShouldReturnOk()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var expectedTask = new TaskDto(
				taskId,
				"Test Task",
				"Test Description",
				TaskItemStatus.Pending,
				DateTimeOffset.UtcNow,
				null);

			_mockTaskService.Setup(s => s.GetTaskByIdAsync(taskId))
				.ReturnsAsync(expectedTask);

			// Act
			var result = await _controller.GetTaskById(taskId);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(expectedTask);
		}

		[Fact]
		public async Task GetTaskById_WhenTaskDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			_mockTaskService.Setup(s => s.GetTaskByIdAsync(taskId))
				.ReturnsAsync((TaskDto?)null);

			// Act
			var result = await _controller.GetTaskById(taskId);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		#endregion

		#region CreateTask Tests

		[Fact]
		public async Task CreateTask_WithValidRequest_ShouldReturnCreatedAtAction()
		{
			// Arrange
			var request = new CreateTaskRequestDto("New Task", "Task Description");
			var createdTask = new TaskDto(
				Guid.NewGuid(),
				request.Title,
				request.Description,
				TaskItemStatus.Pending,
				DateTimeOffset.UtcNow,
				null);

			_mockTaskService.Setup(s => s.CreateTaskAsync(request))
				.ReturnsAsync(createdTask);

			// Act
			var result = await _controller.CreateTask(request);

			// Assert
			var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
			createdResult.ActionName.Should().Be(nameof(TasksController.GetTaskById));
			createdResult.RouteValues!["id"].Should().Be(createdTask.Id);
			createdResult.Value.Should().BeEquivalentTo(createdTask);
		}

		[Fact]
		public async Task CreateTask_ShouldCreateTaskWithPendingStatus()
		{
			// Arrange
			var request = new CreateTaskRequestDto("Task", "Description");
			var createdTask = new TaskDto(
				Guid.NewGuid(),
				request.Title,
				request.Description,
				TaskItemStatus.Pending,
				DateTimeOffset.UtcNow,
				null);

			_mockTaskService.Setup(s => s.CreateTaskAsync(request))
				.ReturnsAsync(createdTask);

			// Act
			var result = await _controller.CreateTask(request);

			// Assert
			var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
			var task = createdResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
			task.Status.Should().Be(TaskItemStatus.Pending);
		}

		#endregion

		#region UpdateTask Tests

		[Fact]
		public async Task UpdateTask_WhenTaskExists_ShouldReturnOk()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new UpdateTaskRequestDto("Updated Task", "Updated Description", TaskItemStatus.InProgress);
			var updatedTask = new TaskDto(
				taskId,
				request.Title,
				request.Description,
				request.Status,
				DateTimeOffset.UtcNow,
				null);

			_mockTaskService.Setup(s => s.UpdateTaskAsync(taskId, request))
				.ReturnsAsync(updatedTask);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(updatedTask);
		}

		[Fact]
		public async Task UpdateTask_WhenTaskDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new UpdateTaskRequestDto("Updated Task", "Description", TaskItemStatus.Completed);

			_mockTaskService.Setup(s => s.UpdateTaskAsync(taskId, request))
				.ReturnsAsync((TaskDto?)null);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task UpdateTask_ShouldUpdateAllFields()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new UpdateTaskRequestDto(
				"Completely New Title",
				"Completely New Description",
				TaskItemStatus.Completed);

			var updatedTask = new TaskDto(
				taskId,
				request.Title,
				request.Description,
				request.Status,
				DateTimeOffset.UtcNow,
				null);

			_mockTaskService.Setup(s => s.UpdateTaskAsync(taskId, request))
				.ReturnsAsync(updatedTask);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var task = okResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
			task.Title.Should().Be(request.Title);
			task.Description.Should().Be(request.Description);
			task.Status.Should().Be(request.Status);
		}

		#endregion

		#region DeleteTask Tests

		[Fact]
		public async Task DeleteTask_WhenTaskExists_ShouldReturnNoContent()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			_mockTaskService.Setup(s => s.DeleteTaskAsync(taskId))
				.ReturnsAsync(true);

			// Act
			var result = await _controller.DeleteTask(taskId);

			// Assert
			result.Should().BeOfType<NoContentResult>();
		}

		[Fact]
		public async Task DeleteTask_WhenTaskDoesNotExist_ShouldReturnNotFound()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			_mockTaskService.Setup(s => s.DeleteTaskAsync(taskId))
				.ReturnsAsync(false);

			// Act
			var result = await _controller.DeleteTask(taskId);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		#endregion
	}
}
