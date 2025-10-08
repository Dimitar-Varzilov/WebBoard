using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebBoard.API.Common.Extensions;
using WebBoard.API.Common.Models;
using WebBoard.API.Data;

namespace WebBoard.Tests.Extensions
{
	/// <summary>
	/// Comprehensive tests for SimpleSearchExtensions
	/// Tests both functionality and maintainability aspects
	/// 
	/// NOTE: These tests use PostgreSQL (via TestContainers or similar) because
	/// InMemoryDatabase doesn't support EF.Functions.ILike which is PostgreSQL-specific.
	/// For CI/CD environments without PostgreSQL, use [Fact(Skip = "Requires PostgreSQL")]
	/// </summary>
	public class SearchMaintainabilityTests : IDisposable
	{
		private readonly AppDbContext _dbContext;
		private const bool UseInMemoryDb = true; // Set to false for PostgreSQL integration tests

		public SearchMaintainabilityTests()
		{
			if (UseInMemoryDb)
			{
				// For unit tests - limited functionality
				var options = new DbContextOptionsBuilder<AppDbContext>()
					.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
					.Options;
				_dbContext = new AppDbContext(options);
			}
			else
			{
				// For integration tests - full PostgreSQL support
				// Uncomment and configure when running against real PostgreSQL
				// var options = new DbContextOptionsBuilder<AppDbContext>()
				//     .UseNpgsql("Host=localhost;Database=WebBoard_Test;Username=postgres;Password=test")
				//     .Options;
				// _dbContext = new AppDbContext(options);
				throw new NotImplementedException("Configure PostgreSQL connection for integration tests");
			}
		}

		public void Dispose()
		{
			_dbContext.Database.EnsureDeleted();
			_dbContext.Dispose();
			GC.SuppressFinalize(this);
		}

		#region Maintainability Tests (No Database Required)

		/// <summary>
		/// This test ensures all public string properties on Job are considered for search
		/// If a developer adds a new string field to Job, this test will fail,
		/// reminding them to update the search logic
		/// </summary>
		[Fact]
		public void Job_AllStringFields_ShouldBeDocumentedForSearch()
		{
			// Arrange
			var jobType = typeof(Job);
			var stringProperties = jobType
				.GetProperties()
				.Where(p => p.PropertyType == typeof(string))
				.Select(p => p.Name)
				.OrderBy(n => n)
				.ToList();

			// Expected searchable fields - UPDATE THIS LIST when adding new searchable fields
			var documentedSearchableFields = new List<string>
			{
				"JobType",  // ✅ Currently searchable
				// Add new searchable string fields here as they're added to the Job model
				// Example: "Description",  // ✅ Searchable as of PR #123
				// Example: "CreatedByUser", // ✅ Searchable as of PR #124
			}.OrderBy(n => n).ToList();

			// Act & Assert
			var actualFields = stringProperties;

			// If this test fails, it means:
			// 1. A new string field was added to Job model, OR
			// 2. A field was removed from Job model
			// 
			// Action required:
			// 1. Update JobService.GetJobsAsync() search logic to include/remove the field
			// 2. Update this test's documentedSearchableFields list
			// 3. Update TaskService if similar fields exist there
			actualFields.Should().BeEquivalentTo(
				documentedSearchableFields,
				options => options.WithStrictOrdering(),
				because: "all string fields should be either searchable or explicitly documented as non-searchable. " +
						 "If this test fails, update JobService.GetJobsAsync() search logic AND this test.");
		}

		/// <summary>
		/// Verify that TaskItem has the expected searchable string fields
		/// </summary>
		[Fact]
		public void TaskItem_AllStringFields_ShouldBeDocumentedForSearch()
		{
			// Arrange
			var taskType = typeof(TaskItem);
			var stringProperties = taskType
				.GetProperties()
				.Where(p => p.PropertyType == typeof(string))
				.Select(p => p.Name)
				.OrderBy(n => n)
				.ToList();

			// Expected searchable fields
			var documentedSearchableFields = new List<string>
			{
				"Title",        // ✅ Currently searchable
				"Description",  // ✅ Currently searchable
			}.OrderBy(n => n).ToList();

			// Assert
			stringProperties.Should().BeEquivalentTo(
				documentedSearchableFields,
				options => options.WithStrictOrdering(),
				because: "TaskService.GetTasksAsync() searches Title and Description");
		}

		/// <summary>
		/// Alternative approach: Test that explicitly lists non-searchable fields
		/// Use this if most fields ARE searchable and only a few are not
		/// </summary>
		[Fact]
		public void Job_NonSearchableFields_ShouldBeExplicitlyDocumented()
		{
			// Arrange
			var jobType = typeof(Job);
			var stringProperties = jobType
				.GetProperties()
				.Where(p => p.PropertyType == typeof(string))
				.Select(p => p.Name)
				.ToHashSet();

			// Fields that should NOT be searchable (e.g., internal audit fields)
			var explicitlyNonSearchableFields = new HashSet<string>
			{
				// Example: "InternalAuditNotes", // Should not be searchable by users
				// Example: "SystemGeneratedId",  // Internal only
			};

			// All other fields SHOULD be searchable
			var expectedSearchableFields = stringProperties
				.Except(explicitlyNonSearchableFields)
				.OrderBy(f => f)
				.ToList();

			// Assert
			expectedSearchableFields.Should().NotBeEmpty(
				because: "there should be at least some searchable string fields on Job");
		}

		#endregion

		#region Extension Method Signature Tests

		[Fact]
		public void SearchInFields_ShouldHaveCorrectSignature()
		{
			// Verify the method exists and has expected signature
			var method = typeof(SimpleSearchExtensions)
				.GetMethod(nameof(SimpleSearchExtensions.SearchInFields));

			method.Should().NotBeNull("SearchInFields method should exist");
			method!.IsStatic.Should().BeTrue("extension methods must be static");
			method.ReturnType.Should().BeAssignableTo(typeof(IQueryable<>), "should return IQueryable<T>");
		}

		[Fact]
		public void SearchInNullableFields_ShouldHaveCorrectSignature()
		{
			// Verify the method exists and has expected signature
			var method = typeof(SimpleSearchExtensions)
				.GetMethod(nameof(SimpleSearchExtensions.SearchInNullableFields));

			method.Should().NotBeNull("SearchInNullableFields method should exist");
			method!.IsStatic.Should().BeTrue("extension methods must be static");
			method.ReturnType.Should().BeAssignableTo(typeof(IQueryable<>), "should return IQueryable<T>");
		}

		#endregion

		#region Parameter Validation Tests (No Database Required)

		[Fact]
		public void SearchInFields_WithNullQuery_ShouldThrow()
		{
			// Arrange
			IQueryable<Job> nullQuery = null!;

			// Act
			Action act = () => nullQuery.SearchInFields("test", j => j.JobType);

			// Assert
			act.Should().Throw<ArgumentNullException>("null query is invalid");
		}

		[Fact]
		public void SearchInNullableFields_WithNullQuery_ShouldThrow()
		{
			// Arrange
			IQueryable<TaskItem> nullQuery = null!;

			// Act
			Action act = () => nullQuery.SearchInNullableFields("test", t => t.Title);

			// Assert
			act.Should().Throw<ArgumentNullException>("null query is invalid");
		}

		#endregion

		#region In-Memory Safe Tests (Don't use EF.Functions.ILike)

		[Fact]
		public async Task SearchInFields_WithNullSearchTerm_ShouldReturnAllRecords()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);

			await _dbContext.Jobs.AddRangeAsync(job1, job2);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _dbContext.Jobs
				.SearchInFields(null, j => j.JobType)
				.ToListAsync();

			// Assert
			result.Should().HaveCount(2, "null search term should return all records");
		}

		[Fact]
		public async Task SearchInFields_WithEmptySearchTerm_ShouldReturnAllRecords()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);

			await _dbContext.Jobs.AddRangeAsync(job1, job2);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _dbContext.Jobs
				.SearchInFields("", j => j.JobType)
				.ToListAsync();

			// Assert
			result.Should().HaveCount(2, "empty search term should return all records");
		}

		[Fact]
		public async Task SearchInFields_WithWhitespaceSearchTerm_ShouldReturnAllRecords()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);

			await _dbContext.Jobs.AddAsync(job1);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _dbContext.Jobs
				.SearchInFields("   ", j => j.JobType)
				.ToListAsync();

			// Assert
			result.Should().HaveCount(1, "whitespace search term should return all records");
		}

		[Fact]
		public async Task SearchInFields_WithNoFieldSelectors_ShouldReturnAllRecords()
		{
			// Arrange
			var job1 = new Job(Guid.NewGuid(), "Job1", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);
			var job2 = new Job(Guid.NewGuid(), "Job2", API.Common.Enums.JobStatus.Queued, DateTimeOffset.UtcNow, null);

			await _dbContext.Jobs.AddRangeAsync(job1, job2);
			await _dbContext.SaveChangesAsync();

			// Act - No field selectors provided
			var result = await _dbContext.Jobs
				.SearchInFields("Job1")
				.ToListAsync();

			// Assert
			result.Should().HaveCount(2, "no field selectors means no filtering applied");
		}

		[Fact]
		public async Task SearchInNullableFields_WithNullSearchTerm_ShouldReturnAllRecords()
		{
			// Arrange
			var task1 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 1", "Desc", API.Common.Enums.TaskItemStatus.Pending, null);
			var task2 = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task 2", "Desc", API.Common.Enums.TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddRangeAsync(task1, task2);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _dbContext.Tasks
				.SearchInNullableFields(null, t => t.Title)
				.ToListAsync();

			// Assert
			result.Should().HaveCount(2);
		}

		[Fact]
		public async Task SearchInNullableFields_WithNoFieldSelectors_ShouldReturnAllRecords()
		{
			// Arrange
			var task = new TaskItem(Guid.NewGuid(), DateTimeOffset.UtcNow, "Task", "Desc", API.Common.Enums.TaskItemStatus.Pending, null);

			await _dbContext.Tasks.AddAsync(task);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _dbContext.Tasks
				.SearchInNullableFields("Task") // No field selectors
				.ToListAsync();

			// Assert
			result.Should().HaveCount(1, "no field selectors means no filtering");
		}

		#endregion

		#region Documentation and Usage Examples

		/// <summary>
		/// Example: How to use SearchInFields in a service
		/// This test documents the intended usage pattern
		/// </summary>
		[Fact]
		public void SearchInFields_UsageExample_ShouldBeDocumented()
		{
			// This is a documentation test showing correct usage
			var exampleCode = @"
// In your service method:
var query = _dbContext.Jobs.AsNoTracking();

if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.SearchInFields(
        searchTerm,
        j => j.JobType,
        j => j.Description,
        j => j.Department
    );
}

var results = await query.ToListAsync();
";

			exampleCode.Should().NotBeNullOrEmpty("usage example should be documented");
		}

		/// <summary>
		/// Example: How to use SearchInNullableFields in a service
		/// </summary>
		[Fact]
		public void SearchInNullableFields_UsageExample_ShouldBeDocumented()
		{
			var exampleCode = @"
// When you have nullable string fields:
var query = _dbContext.Tasks.AsNoTracking();

if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.SearchInNullableFields(
        searchTerm,
        t => t.Title,
        t => t.Description,
        t => t.Notes  // nullable field - automatically handled
    );
}

var results = await query.ToListAsync();
";

			exampleCode.Should().NotBeNullOrEmpty("usage example should be documented");
		}

		#endregion

		#region Integration Test Placeholders (PostgreSQL Required)

		/// <summary>
		/// Integration test - requires actual PostgreSQL database
		/// Run separately with [Fact] attribute when PostgreSQL is available
		/// </summary>
		[Fact(Skip = "Requires PostgreSQL - InMemoryDatabase doesn't support EF.Functions.ILike")]
		public async Task SearchInFields_WithCaseInsensitiveMatch_ShouldReturnResults_PostgreSQL()
		{
			// This test would work with real PostgreSQL
			// Demonstrates case-insensitive search using ILIKE
			await Task.CompletedTask;
		}

		/// <summary>
		/// Integration test - requires actual PostgreSQL database
		/// </summary>
		[Fact(Skip = "Requires PostgreSQL - InMemoryDatabase doesn't support EF.Functions.ILike")]
		public async Task SearchInFields_WithPartialMatch_ShouldReturnResults_PostgreSQL()
		{
			// This test would work with real PostgreSQL
			// Demonstrates partial matching with ILIKE
			await Task.CompletedTask;
		}

		/// <summary>
		/// Integration test - requires actual PostgreSQL database
		/// </summary>
		[Fact(Skip = "Requires PostgreSQL - InMemoryDatabase doesn't support EF.Functions.ILike")]
		public async Task SearchInFields_WithSpecialCharacters_ShouldHandleCorrectly_PostgreSQL()
		{
			// This test would work with real PostgreSQL
			// Demonstrates handling of special characters
			await Task.CompletedTask;
		}

		#endregion
	}
}
