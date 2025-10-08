using FluentAssertions;
using WebBoard.API.Common.DTOs.Tasks;
using WebBoard.API.Common.Enums;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;
using WebBoard.API.Services.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebBoard.Tests
{
	/// <summary>
	/// Tests for completed task read-only validation
	/// </summary>
	public class CompletedTaskValidationTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private readonly TaskService _taskService;

		public CompletedTaskValidationTests()
		{
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

		#region Update Completed Task Tests

		[Fact]
		public async Task UpdateTaskAsync_ShouldThrowException_WhenTaskIsCompleted()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var completedTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Completed Task",
				"This task is done",
				TaskItemStatus.Completed,
				null
			);

			await _dbContext.Tasks.AddAsync(completedTask);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto(
				"New Title",
				"New Description",
				TaskItemStatus.Completed
			);

			// Act
			Func<Task> act = () => _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Cannot update a completed task. Completed tasks are read-only.");
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldSucceed_WhenTaskIsPending()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var pendingTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Pending Task",
				"This task is not done",
				TaskItemStatus.Pending,
				null
			);

			await _dbContext.Tasks.AddAsync(pendingTask);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto(
				"Updated Title",
				"Updated Description",
				TaskItemStatus.InProgress
			);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.Title.Should().Be("Updated Title");
			result.Description.Should().Be("Updated Description");
			result.Status.Should().Be(TaskItemStatus.InProgress);
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldSucceed_WhenTaskIsInProgress()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var inProgressTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"In Progress Task",
				"This task is in progress",
				TaskItemStatus.InProgress,
				null
			);

			await _dbContext.Tasks.AddAsync(inProgressTask);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto(
				"Updated Title",
				"Updated Description",
				TaskItemStatus.Completed
			);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.Status.Should().Be(TaskItemStatus.Completed);
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldNotModifyCompletedTask_EvenWithValidRequest()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var originalTitle = "Original Completed Task";
			var originalDescription = "Original description";
			var completedTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				originalTitle,
				originalDescription,
				TaskItemStatus.Completed,
				null
			);

			await _dbContext.Tasks.AddAsync(completedTask);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto(
				"Attempted New Title",
				"Attempted New Description",
				TaskItemStatus.Completed
			);

			// Act
			Func<Task> act = () => _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>();

			// Verify task remains unchanged
			var unchangedTask = await _dbContext.Tasks.FindAsync(taskId);
			unchangedTask.Should().NotBeNull();
			unchangedTask!.Title.Should().Be(originalTitle);
			unchangedTask.Description.Should().Be(originalDescription);
			unchangedTask.Status.Should().Be(TaskItemStatus.Completed);
		}

		#endregion

		#region Delete Completed Task Tests

		[Fact]
		public async Task DeleteTaskAsync_ShouldThrowException_WhenTaskIsCompleted()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var completedTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Completed Task",
				"This task is done",
				TaskItemStatus.Completed,
				null
			);

			await _dbContext.Tasks.AddAsync(completedTask);
			await _dbContext.SaveChangesAsync();

			// Act
			Func<Task> act = () => _taskService.DeleteTaskAsync(taskId);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Cannot delete a completed task. Completed tasks are read-only.");
		}

		[Fact]
		public async Task DeleteTaskAsync_ShouldSucceed_WhenTaskIsPending()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var pendingTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Pending Task",
				"This task can be deleted",
				TaskItemStatus.Pending,
				null
			);

			await _dbContext.Tasks.AddAsync(pendingTask);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.DeleteTaskAsync(taskId);

			// Assert
			result.Should().BeTrue();
			var deletedTask = await _dbContext.Tasks.FindAsync(taskId);
			deletedTask.Should().BeNull();
		}

		[Fact]
		public async Task DeleteTaskAsync_ShouldSucceed_WhenTaskIsInProgress()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var inProgressTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"In Progress Task",
				"This task can be deleted",
				TaskItemStatus.InProgress,
				null
			);

			await _dbContext.Tasks.AddAsync(inProgressTask);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _taskService.DeleteTaskAsync(taskId);

			// Assert
			result.Should().BeTrue();
			var deletedTask = await _dbContext.Tasks.FindAsync(taskId);
			deletedTask.Should().BeNull();
		}

		[Fact]
		public async Task DeleteTaskAsync_ShouldNotRemoveCompletedTask_FromDatabase()
		{
			// Arrange
			var taskId = Guid.NewGuid();
			var completedTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Completed Task",
				"This task should remain in database",
				TaskItemStatus.Completed,
				null
			);

			await _dbContext.Tasks.AddAsync(completedTask);
			await _dbContext.SaveChangesAsync();

			// Act
			Func<Task> act = () => _taskService.DeleteTaskAsync(taskId);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>();

			// Verify task still exists
			var stillExistingTask = await _dbContext.Tasks.FindAsync(taskId);
			stillExistingTask.Should().NotBeNull();
			stillExistingTask!.Status.Should().Be(TaskItemStatus.Completed);
		}

		#endregion

		#region Edge Case Tests

		[Fact]
		public async Task UpdateTaskAsync_ShouldAllowChangingFromPendingToCompleted()
		{
			// Arrange - This should be allowed (marking task as complete)
			var taskId = Guid.NewGuid();
			var pendingTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Pending Task",
				"About to be completed",
				TaskItemStatus.Pending,
				null
			);

			await _dbContext.Tasks.AddAsync(pendingTask);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto(
				"Completed Task",
				"Now it's done",
				TaskItemStatus.Completed
			);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.Status.Should().Be(TaskItemStatus.Completed);
		}

		[Fact]
		public async Task UpdateTaskAsync_ShouldAllowChangingFromInProgressToCompleted()
		{
			// Arrange - This should be allowed (marking task as complete)
			var taskId = Guid.NewGuid();
			var inProgressTask = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"In Progress Task",
				"About to be completed",
				TaskItemStatus.InProgress,
				null
			);

			await _dbContext.Tasks.AddAsync(inProgressTask);
			await _dbContext.SaveChangesAsync();

			var updateRequest = new UpdateTaskRequestDto(
				"Completed Task",
				"Now it's done",
				TaskItemStatus.Completed
			);

			// Act
			var result = await _taskService.UpdateTaskAsync(taskId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			result!.Status.Should().Be(TaskItemStatus.Completed);
		}

		[Fact]
		public async Task MultipleCompletedTasks_ShouldAllBeProtected()
		{
			// Arrange - Create multiple completed tasks
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc 1", TaskItemStatus.Completed, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc 2", TaskItemStatus.Completed, null);
			var task3 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 3", "Desc 3", TaskItemStatus.Completed, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2, task3);
			await _dbContext.SaveChangesAsync();

			// Act & Assert - All should be protected
			Func<Task> act1 = () => _taskService.DeleteTaskAsync(task1.Id);
			Func<Task> act2 = () => _taskService.DeleteTaskAsync(task2.Id);
			Func<Task> act3 = () => _taskService.DeleteTaskAsync(task3.Id);

			await act1.Should().ThrowAsync<InvalidOperationException>();
			await act2.Should().ThrowAsync<InvalidOperationException>();
			await act3.Should().ThrowAsync<InvalidOperationException>();

			// Verify all still exist
			var count = await _dbContext.Tasks.CountAsync();
			count.Should().Be(3);
		}

		[Fact]
		public async Task CompletedTaskWithJobId_ShouldStillBeProtected()
		{
			// Arrange - Completed task assigned to a job
			var taskId = Guid.NewGuid();
			var jobId = Guid.NewGuid();
			var completedTaskWithJob = new TaskItem(
				taskId,
				DateTimeOffset.UtcNow,
				"Completed Task with Job",
				"This task is completed and has a job",
				TaskItemStatus.Completed,
				jobId
			);

			await _dbContext.Tasks.AddAsync(completedTaskWithJob);
			await _dbContext.SaveChangesAsync();

			// Act & Assert - Should be protected from deletion
			Func<Task> act = () => _taskService.DeleteTaskAsync(taskId);
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Cannot delete a completed task. Completed tasks are read-only.");

			// Act & Assert - Should be protected from update
			var updateRequest = new UpdateTaskRequestDto("New Title", "New Desc", TaskItemStatus.Completed);
			Func<Task> actUpdate = () => _taskService.UpdateTaskAsync(taskId, updateRequest);
			await actUpdate.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Cannot update a completed task. Completed tasks are read-only.");
		}

		#endregion

		#region Integration Tests

		[Fact]
		public async Task CompleteWorkflow_CreateUpdateToCompletedThenProtect()
		{
			// Arrange & Act
			// 1. Create a task
			var createRequest = new CreateTaskRequestDto("New Task", "Description");
			var createdTask = await _taskService.CreateTaskAsync(createRequest);

			createdTask.Status.Should().Be(TaskItemStatus.Pending);

			// 2. Update task to In Progress
			var updateToInProgress = new UpdateTaskRequestDto(
				"Updated Task",
				"Updated Description",
				TaskItemStatus.InProgress
			);
			var inProgressTask = await _taskService.UpdateTaskAsync(createdTask.Id, updateToInProgress);

			inProgressTask.Should().NotBeNull();
			inProgressTask!.Status.Should().Be(TaskItemStatus.InProgress);

			// 3. Mark task as Completed
			var updateToCompleted = new UpdateTaskRequestDto(
				"Completed Task",
				"Final Description",
				TaskItemStatus.Completed
			);
			var completedTask = await _taskService.UpdateTaskAsync(createdTask.Id, updateToCompleted);

			completedTask.Should().NotBeNull();
			completedTask!.Status.Should().Be(TaskItemStatus.Completed);

			// 4. Attempt to update completed task - should fail
			var attemptUpdate = new UpdateTaskRequestDto(
				"Should Fail",
				"Should Fail",
				TaskItemStatus.Pending
			);
			Func<Task> actUpdate = () => _taskService.UpdateTaskAsync(createdTask.Id, attemptUpdate);
			await actUpdate.Should().ThrowAsync<InvalidOperationException>();

			// 5. Attempt to delete completed task - should fail
			Func<Task> actDelete = () => _taskService.DeleteTaskAsync(createdTask.Id);
			await actDelete.Should().ThrowAsync<InvalidOperationException>();

			// 6. Verify task remains completed and unchanged
			var finalTask = await _taskService.GetTaskByIdAsync(createdTask.Id);
			finalTask.Should().NotBeNull();
			finalTask!.Title.Should().Be("Completed Task");
			finalTask.Status.Should().Be(TaskItemStatus.Completed);
		}

		#endregion
	}
}
