using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Tasks;

namespace WebBoard.Tests
{
	/// <summary>
	/// Unit tests for TaskService
	/// </summary>
	public class TaskServiceTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly TaskService _taskService;

		public TaskServiceTests()
		{
			// Setup in-memory database
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			_dbContext = new AppDbContext(options);
			_taskService = new TaskService(_dbContext);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
		}

		#region CreateTaskAsync Tests

		[Fact]
		public async Task CreateTaskAsync_ShouldCreateTask_WithValidData()
		{
			// Arrange
			var request = new CreateTaskRequestDto("Test Task", "Test Description");

			// Act
			var result = await _taskService.CreateTaskAsync(request);

			// Assert
			result.Should().NotBeNull();
			result.Title.Should().Be("Test Task");
			result.Description.Should().Be("Test Description");
			result.Status.Should().Be(TaskItemStatus.Pending);
			result.JobId.Should().BeNull();
			result.Id.Should().NotBeEmpty();
		}

		[Fact]
		public async Task CreateTaskAsync_ShouldPersistTaskToDatabase()
		{
			// Arrange
			var request = new CreateTaskRequestDto("New Task", "New Description");

			// Act
			var result = await _taskService.CreateTaskAsync(request);

			// Assert
			var savedTask = await _dbContext.Tasks.FindAsync(result.Id);
			savedTask.Should().NotBeNull();
			savedTask!.Title.Should().Be("New Task");
			savedTask.Description.Should().Be("New Description");
			savedTask.Status.Should().Be(TaskItemStatus.Pending);
		}

		[Fact]
		public async Task CreateTaskAsync_ShouldSetCreatedAtToCurrentTime()
		{
			// Arrange
			var request = new CreateTaskRequestDto("Task", "Description");
			var beforeCreation = DateTimeOffset.UtcNow.AddSeconds(-1);

			// Act
			var result = await _taskService.CreateTaskAsync(request);

			// Assert
			var afterCreation = DateTimeOffset.UtcNow.AddSeconds(1);
			result.CreatedAt.Should().BeAfter(beforeCreation);
			result.CreatedAt.Should().BeBefore(afterCreation);
		}

		[Fact]
		public async Task CreateTaskAsync_ShouldGenerateUniqueIds()
		{
			// Arrange
			var request1 = new CreateTaskRequestDto("Task 1", "Description 1");
			var request2 = new CreateTaskRequestDto("Task 2", "Description 2");

			// Act
			var result1 = await _taskService.CreateTaskAsync(request1);
			var result2 = await _taskService.CreateTaskAsync(request2);

			// Assert
			result1.Id.Should().NotBe(result2.Id);
		}

		#endregion

		#region GetTasksAsync Tests

		[Fact]
		public async Task GetTasksAsync_ShouldReturnEmptyResult_WhenNoTasksExist()
		{
			// Arrange
			var parameters = new TaskQueryParameters();

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().BeEmpty();
			result.Metadata.TotalCount.Should().Be(0);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldReturnAllTasks_WhenNoFiltersApplied()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc 1", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc 2", TaskItemStatus.Completed, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters();

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(2);
			result.Metadata.TotalCount.Should().Be(2);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldFilterByStatus()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Completed, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { Status = (int)TaskItemStatus.Pending };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(2);
			result.Items.Should().OnlyContain(t => t.Status == TaskItemStatus.Pending);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldFilterByHasJob_True()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, jobId);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { HasJob = true };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(1);
			result.Items.First().JobId.Should().Be(jobId);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldFilterByHasJob_False()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, jobId);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Pending, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { HasJob = false };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(2);
			result.Items.Should().OnlyContain(t => t.JobId == null);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldFilterBySearchTerm_InTitle()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Important Task", "Description", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Regular Task", "Description", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { SearchTerm = "important" };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(1);
			result.Items.First().Title.Should().Be("Important Task");
		}

		[Fact]
		public async Task GetTasksAsync_ShouldFilterBySearchTerm_InDescription()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Urgent description", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Normal description", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { SearchTerm = "urgent" };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(1);
			result.Items.First().Description.Should().Be("Urgent description");
		}

		[Fact]
		public async Task GetTasksAsync_ShouldBeCaseInsensitive_ForSearchTerm()
		{
			// Arrange
			var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Important TASK", "Description", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { SearchTerm = "IMPORTANT" };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(1);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldApplyPagination()
		{
			// Arrange
			for (int i = 0; i < 15; i++)
			{
				var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, $"Task {i}", "Description", TaskItemStatus.Pending, null);
				await _dbContext.Tasks.AddAsync(task);
			}
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters { PageNumber = 2, PageSize = 5 };

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(5);
			result.Metadata.TotalCount.Should().Be(15);
			result.Metadata.CurrentPage.Should().Be(2);
			result.Metadata.PageSize.Should().Be(5);
			result.Metadata.TotalPages.Should().Be(3);
		}

		[Fact]
		public async Task GetTasksAsync_ShouldApplySorting_Descending()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-2), "Oldest", "Desc", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), "Middle", "Desc", TaskItemStatus.Pending, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Newest", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters(); // Default is descending by CreatedAt

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(3);
			result.Items.First().Title.Should().Be("Newest");
			result.Items.Last().Title.Should().Be("Oldest");
		}

		[Fact]
		public async Task GetTasksAsync_WithMultipleFilters_ShouldApplyAllFilters()
		{
			// Arrange
			var jobId = Guid.NewGuid();
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Important Task", "Urgent", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Important Work", "Normal", TaskItemStatus.Completed, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Regular Task", "Urgent", TaskItemStatus.Pending, jobId);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			var parameters = new TaskQueryParameters
			{
				Status = (int)TaskItemStatus.Pending,
				HasJob = false,
				SearchTerm = "important"
			};

			// Act
			var result = await _taskService.GetTasksAsync(parameters);

			// Assert
			result.Items.Should().HaveCount(1);
			result.Items.First().Title.Should().Be("Important Task");
		}

		#endregion

		#region GetTasksByStatusAsync Tests

		[Fact]
		public async Task GetTasksByStatusAsync_ShouldReturnTasksWithSpecifiedStatus()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Completed, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTasksByStatusAsync(TaskItemStatus.Pending);

			// Assert
			result.Should().HaveCount(2);
			result.Should().OnlyContain(t => t.Status == TaskItemStatus.Pending);
		}

		[Fact]
		public async Task GetTasksByStatusAsync_ShouldReturnEmptyList_WhenNoTasksMatch()
		{
			// Arrange
			var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTasksByStatusAsync(TaskItemStatus.Completed);

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTasksByStatusAsync_ShouldOrderByCreatedAt()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-2), "Oldest", "Desc", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Newest", "Desc", TaskItemStatus.Pending, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), "Middle", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTasksByStatusAsync(TaskItemStatus.Pending);

			// Assert
			result.Should().HaveCount(3);
			result.ElementAt(0).Title.Should().Be("Oldest");
			result.ElementAt(1).Title.Should().Be("Middle");
			result.ElementAt(2).Title.Should().Be("Newest");
		}

		#endregion

		#region GetTaskCountByStatusAsync Tests

		[Fact]
		public async Task GetTaskCountByStatusAsync_ShouldReturnCorrectCount()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", TaskItemStatus.Pending, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc", TaskItemStatus.Completed, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTaskCountByStatusAsync(TaskItemStatus.Pending);

			// Assert
			result.Should().Be(2);
		}

		[Fact]
		public async Task GetTaskCountByStatusAsync_ShouldReturnZero_WhenNoTasksMatch()
		{
			// Arrange
			var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);
			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTaskCountByStatusAsync(TaskItemStatus.Completed);

			// Assert
			result.Should().Be(0);
		}

		[Theory]
		[InlineData(TaskItemStatus.Pending)]
		[InlineData(TaskItemStatus.Completed)]
		[InlineData(TaskItemStatus.InProgress)]
		public async Task GetTaskCountByStatusAsync_ShouldWorkForAllStatuses(TaskItemStatus status)
		{
			// Arrange
			var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", status, null);
			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTaskCountByStatusAsync(status);

			// Assert
			result.Should().Be(1);
		}

		#endregion

		#region GetTaskByIdAsync Tests

		[Fact]
		public async Task GetTaskByIdAsync_ShouldReturnNull_WhenTaskDoesNotExist()
		{
			// Arrange
			var nonExistentId = Guid.NewGuid();

			// Act
			var result = await _taskService.GetTaskByIdAsync(nonExistentId);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetTaskByIdAsync_ShouldReturnTask_WhenTaskExists()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Test Task", "Description", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTaskByIdAsync(taskId);

			// Assert
			result.Should().NotBeNull();
			result!.Id.Should().Be(taskId);
			result.Title.Should().Be("Test Task");
			result.Description.Should().Be("Description");
			result.Status.Should().Be(TaskItemStatus.Pending);
		}

		[Fact]
		public async Task GetTaskByIdAsync_ShouldIncludeJobId_WhenTaskIsAssignedToJob()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var jobId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, jobId);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.GetTaskByIdAsync(taskId);

			// Assert
			result.Should().NotBeNull();
			result!.JobId.Should().Be(jobId);
		}

		#endregion

		#region UpdateTaskAsync Tests

		[Fact]
		public async Task UpdateTaskAsync_ShouldReturnNull_WhenTaskDoesNotExist()
		{
			// Arrange
			var nonExistentId = Guid.NewGuid();
			var updateRequest = new UpdateTaskRequestDto("Updated", "Updated Description", TaskItemStatus.Completed);

			// Act
			var result = await _taskService.UpdateTaskAsync(nonExistentId, updateRequest);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldUpdateTask_WhenTaskExists()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Original", "Original Description", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto("Updated Title", "Updated Description", TaskItemStatus.Completed);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.Title.Should().Be("Updated Title");
			result.Description.Should().Be("Updated Description");
			result.Status.Should().Be(TaskItemStatus.Completed);
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldPersistChangesToDatabase()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Original", "Original Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto("Updated", "Updated Desc", TaskItemStatus.Completed);

			// Act
			await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			var updatedTask = await _dbContext.Tasks.FindAsync(taskId);
			updatedTask.Should().NotBeNull();
			updatedTask!.Title.Should().Be("Updated");
			updatedTask.Description.Should().Be("Updated Desc");
			updatedTask.Status.Should().Be(TaskItemStatus.Completed);
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldPreserveCreatedAt()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-5);
			var task = new TaskItem(taskId, originalCreatedAt, "Original", "Original Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto("Updated", "Updated Desc", TaskItemStatus.Completed);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.CreatedAt.Should().Be(originalCreatedAt);
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldPreserveJobId()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var jobId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Original", "Original Desc", TaskItemStatus.Pending, jobId);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto("Updated", "Updated Desc", TaskItemStatus.Completed);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.JobId.Should().Be(jobId);
		}

		[Theory]
		[InlineData(TaskItemStatus.Pending)]
		[InlineData(TaskItemStatus.Completed)]
		[InlineData(TaskItemStatus.InProgress)]
		public async Task UpdateTaskAsync_ShouldUpdateToAllStatuses(TaskItemStatus newStatus)
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Task", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto("Task", "Desc", newStatus);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.Status.Should().Be(newStatus);
		}

		#endregion

		#region DeleteTaskAsync Tests

		[Fact]
		public async Task DeleteTaskAsync_ShouldReturnFalse_WhenTaskDoesNotExist()
		{
			// Arrange
			var nonExistentId = Guid.NewGuid();

			// Act
			var result = await _taskService.DeleteTaskAsync(nonExistentId);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task DeleteTaskAsync_ShouldReturnTrue_WhenTaskExists()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Task to Delete", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.DeleteTaskAsync(taskId);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task DeleteTaskAsync_ShouldRemoveTaskFromDatabase()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var task = new TaskItem(taskId, DateTimeOffset.UtcNow, "Task to Delete", "Desc", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			await _taskService.DeleteTaskAsync(taskId);

			// Assert
			var deletedTask = await _dbContext.Tasks.FindAsync(taskId);
			deletedTask.Should().BeNull();
		}

		[Fact]
		public async Task DeleteTaskAsync_ShouldNotAffectOtherTasks()
		{
			// Arrange
			var taskId1 = Guid.NewGuid();
			var taskId2 = Guid.NewGuid();
			var task1 = new TaskItem(taskId1, DateTimeOffset.UtcNow, "Task 1", "Desc 1", TaskItemStatus.Pending, null);
			var task2 = new TaskItem(taskId2, DateTimeOffset.UtcNow, "Task 2", "Desc 2", TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2);
			await _dbContext.SaveChangesAsync();

			// Act
			await _taskService.DeleteTaskAsync(taskId1);

			// Assert
			var remainingTask = await _dbContext.Tasks.FindAsync(taskId2);
			remainingTask.Should().NotBeNull();
			var taskCount = await _dbContext.Tasks.CountAsync();
			taskCount.Should().Be(1);
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task CreateAndRetrieveTask_ShouldWorkCorrectly()
		{
			// Arrange
			var createRequest = new CreateTaskRequestDto("Integration Task", "Integration Description");

			// Act
			var createdTask = await _taskService.CreateTaskAsync(createRequest);
			var retrievedTask = await _taskService.GetTaskByIdAsync(createdTask.Id);

			// Assert
			retrievedTask.Should().NotBeNull();
			retrievedTask!.Id.Should().Be(createdTask.Id);
			retrievedTask.Title.Should().Be(createdTask.Title);
			retrievedTask.Description.Should().Be(createdTask.Description);
			retrievedTask.Status.Should().Be(createdTask.Status);
		}

		[Fact]
		public async Task CreateUpdateAndRetrieveTask_ShouldWorkCorrectly()
		{
			// Arrange
			var createRequest = new CreateTaskRequestDto("Original Task", "Original Description");

			// Act
			var createdTask = await _taskService.CreateTaskAsync(createRequest);
			var updateRequest = new UpdateTaskRequestDto("Updated Task", "Updated Description", TaskItemStatus.Completed);
			await _taskService.UpdateTaskAsync(createdTask.Id, updateRequest);
			var retrievedTask = await _taskService.GetTaskByIdAsync(createdTask.Id);

			// Assert
			retrievedTask.Should().NotBeNull();
			retrievedTask!.Title.Should().Be("Updated Task");
			retrievedTask.Description.Should().Be("Updated Description");
			retrievedTask.Status.Should().Be(TaskItemStatus.Completed);
		}

		[Fact]
		public async Task CreateAndDeleteTask_ShouldWorkCorrectly()
		{
			// Arrange
			var createRequest = new CreateTaskRequestDto("Task to Delete", "Description");

			// Act
			var createdTask = await _taskService.CreateTaskAsync(createRequest);
			var deleteResult = await _taskService.DeleteTaskAsync(createdTask.Id);
			var retrievedTask = await _taskService.GetTaskByIdAsync(createdTask.Id);

			// Assert
			deleteResult.Should().BeTrue();
			retrievedTask.Should().BeNull();
		}

		[Fact]
		public async Task CompleteWorkflow_CreateMultipleTasksFilterAndUpdate_ShouldWorkCorrectly()
		{
			// Arrange & Act
			await _taskService.CreateTaskAsync(new CreateTaskRequestDto("Urgent Task", "High priority"));
			var task2 = await _taskService.CreateTaskAsync(new CreateTaskRequestDto("Normal Task", "Regular work"));
			await _taskService.CreateTaskAsync(new CreateTaskRequestDto("Urgent Work", "Also high priority"));

			// Update one task to completed
			await _taskService.UpdateTaskAsync(task2.Id, new UpdateTaskRequestDto("Normal Task", "Regular work", TaskItemStatus.Completed));

			// Filter by status
			var pendingTasks = await _taskService.GetTasksByStatusAsync(TaskItemStatus.Pending);

			// Search for urgent tasks
			var urgentTasks = await _taskService.GetTasksAsync(new TaskQueryParameters { SearchTerm = "urgent" });

			// Assert
			pendingTasks.Should().HaveCount(2);
			urgentTasks.Items.Should().HaveCount(2);

			var countPending = await _taskService.GetTaskCountByStatusAsync(TaskItemStatus.Pending);
			var countCompleted = await _taskService.GetTaskCountByStatusAsync(TaskItemStatus.Completed);

			countPending.Should().Be(2);
			countCompleted.Should().Be(1);
		}

		#endregion
	}
}
