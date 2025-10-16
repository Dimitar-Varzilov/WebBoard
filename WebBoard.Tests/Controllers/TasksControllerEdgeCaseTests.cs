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
	/// <summary>
	/// Additional edge case tests for TasksController to improve coverage
	/// </summary>
	public class TasksControllerEdgeCaseTests
	{
		private readonly Mock<ITaskService> _mockTaskService;
		private readonly TasksController _controller;

		public TasksControllerEdgeCaseTests()
		{
			_mockTaskService = new Mock<ITaskService>();
			_controller = new TasksController(_mockTaskService.Object);
		}

		#region GetTasksByStatus Edge Cases

		[Theory]
		[InlineData(TaskItemStatus.Pending)]
		[InlineData(TaskItemStatus.InProgress)]
		[InlineData(TaskItemStatus.Completed)]
		public async Task GetTasksByStatus_WithValidEnum_ShouldReturnOk(TaskItemStatus status)
		{
			// Arrange
			var tasks = new List<TaskDto>
			{
				new(Guid.NewGuid(), "Task", "Description", status, DateTimeOffset.UtcNow)
			};

			_mockTaskService.Setup(s => s.GetTasksByStatusAsync(status))
				.ReturnsAsync(tasks);

			// Act
			var result = await _controller.GetTasksByStatus(status);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(tasks);
		}

		[Fact]
		public async Task GetTasksByStatus_WithInvalidEnum_ShouldReturnBadRequest()
		{
			// Arrange
			var invalidStatus = (TaskItemStatus)999;

			// Act
			var result = await _controller.GetTasksByStatus(invalidStatus);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().NotBeNull();
			badRequestResult.Value!.ToString().Should().Contain("Invalid status");
		}

		[Fact]
		public async Task GetTasksByStatus_ShouldIncludeValidStatusesInErrorMessage()
		{
			// Arrange
			var invalidStatus = (TaskItemStatus)(-1);

			// Act
			var result = await _controller.GetTasksByStatus(invalidStatus);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			var message = badRequestResult?.Value?.ToString();
			message.Should().NotBeNullOrEmpty();
			message.Should().Contain("Pending");
			message.Should().Contain("InProgress");
			message.Should().Contain("Completed");
		}

		#endregion

		#region GetTaskCountByStatus Edge Cases

		[Theory]
		[InlineData(TaskItemStatus.Pending, 5)]
		[InlineData(TaskItemStatus.InProgress, 0)]
		[InlineData(TaskItemStatus.Completed, 100)]
		public async Task GetTaskCountByStatus_WithValidEnum_ShouldReturnCount(TaskItemStatus status, int count)
		{
			// Arrange
			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(status))
				.ReturnsAsync(count);

			// Act
			var result = await _controller.GetTaskCountByStatus(status);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().Be(count);
		}

		[Fact]
		public async Task GetTaskCountByStatus_WithInvalidEnum_ShouldReturnBadRequest()
		{
			// Arrange
			var invalidStatus = (TaskItemStatus)500;

			// Act
			var result = await _controller.GetTaskCountByStatus(invalidStatus);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task GetTaskCountByStatus_WithZeroCount_ShouldReturnZero()
		{
			// Arrange
			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(TaskItemStatus.Pending))
				.ReturnsAsync(0);

			// Act
			var result = await _controller.GetTaskCountByStatus(TaskItemStatus.Pending);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().Be(0);
		}

		#endregion

		#region CreateTask Edge Cases

		[Fact]
		public async Task CreateTask_WithInvalidModelState_ShouldReturnBadRequest()
		{
			// Arrange
			_controller.ModelState.AddModelError("Title", "Title is required");
			var request = new CreateTaskRequestDto("", "Description");

			// Act
			var result = await _controller.CreateTask(request);

			// Assert
			var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badRequestResult.Value.Should().BeOfType<SerializableError>();
		}

		[Fact]
		public async Task CreateTask_WithEmptyTitle_ShouldValidateAndReturnBadRequest()
		{
			// Arrange
			_controller.ModelState.AddModelError("Title", "Title cannot be empty");
			var request = new CreateTaskRequestDto("", "Valid Description");

			// Act
			var result = await _controller.CreateTask(request);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task CreateTask_WithValidData_ShouldReturnCreatedAtAction()
		{
			// Arrange
			var request = new CreateTaskRequestDto("New Task", "New Description");
			var createdTask = new TaskDto(Guid.NewGuid(), "New Task", "New Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow);

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
		public async Task CreateTask_ShouldSetCorrectLocationHeader()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new CreateTaskRequestDto("Task", "Description");
			var createdTask = new TaskDto(taskId, "Task", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow);

			_mockTaskService.Setup(s => s.CreateTaskAsync(request))
				.ReturnsAsync(createdTask);

			// Act
			var result = await _controller.CreateTask(request);

			// Assert
			var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
			createdResult.RouteValues!["id"].Should().Be(taskId);
		}

		#endregion

		#region UpdateTask Edge Cases

		[Fact]
		public async Task UpdateTask_WithInvalidModelState_ShouldReturnBadRequest()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			_controller.ModelState.AddModelError("Title", "Title is required");
			var request = new UpdateTaskRequestDto("", "Description", TaskItemStatus.Pending);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
		}

		[Fact]
		public async Task UpdateTask_WithNonExistentTask_ShouldReturnNotFound()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new UpdateTaskRequestDto("Updated", "Description", TaskItemStatus.Completed);

			_mockTaskService.Setup(s => s.UpdateTaskAsync(taskId, request))
				.ReturnsAsync((TaskDto?)null);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task UpdateTask_WithValidData_ShouldReturnOk()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new UpdateTaskRequestDto("Updated Task", "Updated Description", TaskItemStatus.Completed);
			var updatedTask = new TaskDto(taskId, "Updated Task", "Updated Description", TaskItemStatus.Completed, DateTimeOffset.UtcNow);

			_mockTaskService.Setup(s => s.UpdateTaskAsync(taskId, request))
				.ReturnsAsync(updatedTask);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(updatedTask);
		}

		[Theory]
		[InlineData(TaskItemStatus.Pending)]
		[InlineData(TaskItemStatus.InProgress)]
		[InlineData(TaskItemStatus.Completed)]
		public async Task UpdateTask_WithDifferentStatuses_ShouldUpdateCorrectly(TaskItemStatus status)
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var request = new UpdateTaskRequestDto("Task", "Description", status);
			var updatedTask = new TaskDto(taskId, "Task", "Description", status, DateTimeOffset.UtcNow);

			_mockTaskService.Setup(s => s.UpdateTaskAsync(taskId, request))
				.ReturnsAsync(updatedTask);

			// Act
			var result = await _controller.UpdateTask(taskId, request);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var returnedTask = okResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
			returnedTask.Status.Should().Be(status);
		}

		#endregion

		#region DeleteTask Edge Cases

		[Fact]
		public async Task DeleteTask_WithNonExistentTask_ShouldReturnNotFound()
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

		[Fact]
		public async Task DeleteTask_WithExistingTask_ShouldReturnNoContent()
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
		public async Task DeleteTask_ShouldCallServiceOnce()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			_mockTaskService.Setup(s => s.DeleteTaskAsync(taskId))
				.ReturnsAsync(true);

			// Act
			await _controller.DeleteTask(taskId);

			// Assert
			_mockTaskService.Verify(s => s.DeleteTaskAsync(taskId), Times.Once);
		}

		#endregion

		#region GetTasks Edge Cases

		[Fact]
		public async Task GetTasks_WithEmptyResult_ShouldReturnEmptyPagedResult()
		{
			// Arrange
			var parameters = new TaskQueryParameters();
			var emptyResult = new PagedResult<TaskDto>([], 0, 1, 10);

			_mockTaskService.Setup(s => s.GetTasksAsync(parameters))
				.ReturnsAsync(emptyResult);

			// Act
			var result = await _controller.GetTasks(parameters);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			var pagedResult = okResult.Value.Should().BeAssignableTo<PagedResult<TaskDto>>().Subject;
			pagedResult.Items.Should().BeEmpty();
			pagedResult.Metadata.TotalCount.Should().Be(0);
		}

		[Fact]
		public async Task GetTasks_WithFilters_ShouldPassParametersToService()
		{
			// Arrange
			var parameters = new TaskQueryParameters
			{
				Status = (int)TaskItemStatus.Pending,
				HasJob = false,
				Filters = "test",
				Page = 2,
				PageSize = 20
			};

			var result = new PagedResult<TaskDto>([], 0, 2, 20);
			_mockTaskService.Setup(s => s.GetTasksAsync(parameters))
				.ReturnsAsync(result);

			// Act
			await _controller.GetTasks(parameters);

			// Assert
			_mockTaskService.Verify(s => s.GetTasksAsync(
				It.Is<TaskQueryParameters>(p =>
					p.Status == parameters.Status &&
					p.HasJob == parameters.HasJob &&
					p.Filters == parameters.Filters &&
					p.Page == parameters.Page &&
					p.PageSize == parameters.PageSize)),
				Times.Once);
		}

		#endregion

		#region GetTaskById Edge Cases

		[Fact]
		public async Task GetTaskById_WithEmptyGuid_ShouldCallService()
		{
			// Arrange
			var emptyGuid = Guid.Empty;
			_mockTaskService.Setup(s => s.GetTaskByIdAsync(emptyGuid))
				.ReturnsAsync((TaskDto?)null);

			// Act
			var result = await _controller.GetTaskById(emptyGuid);

			// Assert
			result.Should().BeOfType<NotFoundResult>();
			_mockTaskService.Verify(s => s.GetTaskByIdAsync(emptyGuid), Times.Once);
		}

		[Fact]
		public async Task GetTaskById_WithValidGuid_ShouldReturnTask()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskDto(taskId, "Task", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow);

			_mockTaskService.Setup(s => s.GetTaskByIdAsync(taskId))
				.ReturnsAsync(task);

			// Act
			var result = await _controller.GetTaskById(taskId);

			// Assert
			var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(task);
		}

		#endregion

		#region Integration Scenarios

		[Fact]
		public async Task GetTasksByStatus_ThenGetCount_ShouldUseCorrectStatus()
		{
			// Arrange
			var status = TaskItemStatus.InProgress;
			var tasks = new List<TaskDto>
			{
				new(Guid.NewGuid(), "Task1", "Desc", status, DateTimeOffset.UtcNow),
				new(Guid.NewGuid(), "Task2", "Desc", status, DateTimeOffset.UtcNow)
			};

			_mockTaskService.Setup(s => s.GetTasksByStatusAsync(status))
				.ReturnsAsync(tasks);
			_mockTaskService.Setup(s => s.GetTaskCountByStatusAsync(status))
				.ReturnsAsync(tasks.Count);

			// Act
			var tasksResult = await _controller.GetTasksByStatus(status);
			var countResult = await _controller.GetTaskCountByStatus(status);

			// Assert
			var okTasksResult = tasksResult.Should().BeOfType<OkObjectResult>().Subject;
			var returnedTasks = okTasksResult.Value.Should().BeAssignableTo<IEnumerable<TaskDto>>().Subject;
			returnedTasks.Should().HaveCount(2);

			var okCountResult = countResult.Should().BeOfType<OkObjectResult>().Subject;
			okCountResult.Value.Should().Be(2);
		}

		[Fact]
		public async Task CreateTask_ThenGetById_ShouldReturnCreatedTask()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var createRequest = new CreateTaskRequestDto("New Task", "Description");
			var createdTask = new TaskDto(taskId, "New Task", "Description", TaskItemStatus.Pending, DateTimeOffset.UtcNow);

			_mockTaskService.Setup(s => s.CreateTaskAsync(createRequest))
				.ReturnsAsync(createdTask);
			_mockTaskService.Setup(s => s.GetTaskByIdAsync(taskId))
				.ReturnsAsync(createdTask);

			// Act
			var createResult = await _controller.CreateTask(createRequest);
			var getResult = await _controller.GetTaskById(taskId);

			// Assert
			var createdAtResult = createResult.Should().BeOfType<CreatedAtActionResult>().Subject;
			createdAtResult.Value.Should().BeEquivalentTo(createdTask);

			var okResult = getResult.Should().BeOfType<OkObjectResult>().Subject;
			okResult.Value.Should().BeEquivalentTo(createdTask);
		}

		#endregion
	}
}
