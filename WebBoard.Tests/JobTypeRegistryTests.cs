using FluentAssertions;
using Quartz;
using WebBoard.API.Common.Constants;
using WebBoard.API.Services.Jobs;

namespace WebBoard.Tests
{
	public class JobTypeRegistryTests
	{
		private readonly JobTypeRegistry _registry;

		public JobTypeRegistryTests()
		{
			_registry = new JobTypeRegistry();
		}

		#region GetJobType Tests

		[Fact]
		public void GetJobType_ShouldReturnCorrectType_ForMarkAllTasksAsDone()
		{
			// Act
			var result = _registry.GetJobType(Constants.JobTypes.MarkAllTasksAsDone);

			// Assert
			result.Should().Be(typeof(MarkTasksAsCompletedJob));
		}

		[Fact]
		public void GetJobType_ShouldReturnCorrectType_ForGenerateTaskReport()
		{
			// Act
			var result = _registry.GetJobType(Constants.JobTypes.GenerateTaskReport);

			// Assert
			result.Should().Be(typeof(GenerateTaskListJob));
		}

		[Fact]
		public void GetJobType_ShouldThrowArgumentException_WhenJobTypeNameIsNull()
		{
			// Act
			Action act = () => _registry.GetJobType(null!);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("*Job type name cannot be null or empty*");
		}

		[Fact]
		public void GetJobType_ShouldThrowArgumentException_WhenJobTypeNameIsEmpty()
		{
			// Act
			Action act = () => _registry.GetJobType(string.Empty);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("*Job type name cannot be null or empty*");
		}

		[Fact]
		public void GetJobType_ShouldThrowArgumentException_WhenJobTypeNameIsWhitespace()
		{
			// Act
			Action act = () => _registry.GetJobType("   ");

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("*Job type name cannot be null or empty*");
		}

		[Fact]
		public void GetJobType_ShouldThrowInvalidOperationException_WhenJobTypeNotFound()
		{
			// Arrange
			var unknownJobType = "UnknownJobType";

			// Act
			Action act = () => _registry.GetJobType(unknownJobType);

			// Assert
			act.Should().Throw<InvalidOperationException>()
				.WithMessage($"*Unknown job type: '{unknownJobType}'*");
		}

		[Fact]
		public void GetJobType_ErrorMessage_ShouldIncludeAvailableTypes()
		{
			// Arrange
			var unknownJobType = "UnknownJobType";

			// Act
			Action act = () => _registry.GetJobType(unknownJobType);

			// Assert
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*Available types:*");
		}

		#endregion

		#region IsValidJobType Tests

		[Fact]
		public void IsValidJobType_ShouldReturnTrue_ForMarkAllTasksAsDone()
		{
			// Act
			var result = _registry.IsValidJobType(Constants.JobTypes.MarkAllTasksAsDone);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IsValidJobType_ShouldReturnTrue_ForGenerateTaskReport()
		{
			// Act
			var result = _registry.IsValidJobType(Constants.JobTypes.GenerateTaskReport);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IsValidJobType_ShouldReturnFalse_ForUnknownType()
		{
			// Act
			var result = _registry.IsValidJobType("UnknownJobType");

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValidJobType_ShouldReturnFalse_ForNull()
		{
			// Act
			var result = _registry.IsValidJobType(null!);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValidJobType_ShouldReturnFalse_ForEmptyString()
		{
			// Act
			var result = _registry.IsValidJobType(string.Empty);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValidJobType_ShouldReturnFalse_ForWhitespace()
		{
			// Act
			var result = _registry.IsValidJobType("   ");

			// Assert
			result.Should().BeFalse();
		}

		#endregion

		#region GetAllJobTypes Tests

		[Fact]
		public void GetAllJobTypes_ShouldReturnAllRegisteredTypes()
		{
			// Act
			var result = _registry.GetAllJobTypes();

			// Assert
			result.Should().Contain(Constants.JobTypes.MarkAllTasksAsDone);
			result.Should().Contain(Constants.JobTypes.GenerateTaskReport);
		}

		[Fact]
		public void GetAllJobTypes_ShouldReturnNonEmptyCollection()
		{
			// Act
			var result = _registry.GetAllJobTypes();

			// Assert
			result.Should().NotBeEmpty();
			result.Count().Should().BeGreaterThanOrEqualTo(2);
		}

		#endregion

		#region RegisterJobType Tests

		[Fact]
		public void RegisterJobType_ShouldAddNewJobType()
		{
			// Arrange
			var newJobTypeName = "CustomJob";

			// Act
			_registry.RegisterJobType(newJobTypeName, typeof(MarkTasksAsCompletedJob));

			// Assert
			_registry.IsValidJobType(newJobTypeName).Should().BeTrue();
			_registry.GetJobType(newJobTypeName).Should().Be(typeof(MarkTasksAsCompletedJob));
		}

		[Fact]
		public void RegisterJobType_ShouldOverwriteExistingJobType()
		{
			// Arrange
			var jobTypeName = "TestJob";
			_registry.RegisterJobType(jobTypeName, typeof(MarkTasksAsCompletedJob));

			// Act
			_registry.RegisterJobType(jobTypeName, typeof(GenerateTaskListJob));

			// Assert
			_registry.GetJobType(jobTypeName).Should().Be(typeof(GenerateTaskListJob));
		}

		[Fact]
		public void RegisterJobType_ShouldThrowArgumentException_WhenJobTypeNameIsNull()
		{
			// Act
			Action act = () => _registry.RegisterJobType(null!, typeof(MarkTasksAsCompletedJob));

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("*Job type name cannot be null or empty*");
		}

		[Fact]
		public void RegisterJobType_ShouldThrowArgumentException_WhenJobTypeNameIsEmpty()
		{
			// Act
			Action act = () => _registry.RegisterJobType(string.Empty, typeof(MarkTasksAsCompletedJob));

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("*Job type name cannot be null or empty*");
		}

		[Fact]
		public void RegisterJobType_ShouldThrowArgumentNullException_WhenJobTypeIsNull()
		{
			// Act
			Action act = () => _registry.RegisterJobType("TestJob", null!);

			// Assert
			act.Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void RegisterJobType_ShouldThrowArgumentException_WhenTypeDoesNotImplementIJob()
		{
			// Act
			Action act = () => _registry.RegisterJobType("InvalidJob", typeof(string));

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("*must implement IJob interface*");
		}

		#endregion

		#region Integration Tests

		[Fact]
		public void JobTypeRegistry_ShouldDiscoverJobsWithAttribute()
		{
			// Act
			var allTypes = _registry.GetAllJobTypes().ToList();

			// Assert
			allTypes.Should().Contain(Constants.JobTypes.MarkAllTasksAsDone);
			allTypes.Should().Contain(Constants.JobTypes.GenerateTaskReport);

			// Verify all discovered types implement IJob
			foreach (var jobTypeName in allTypes)
			{
				var jobType = _registry.GetJobType(jobTypeName);
				jobType.Should().BeAssignableTo<IJob>();
			}
		}

		[Fact]
		public void JobTypeRegistry_ShouldWorkWithRoundTrip()
		{
			// Arrange
			var customJobName = "RoundTripJob";

			// Act - Register
			_registry.RegisterJobType(customJobName, typeof(MarkTasksAsCompletedJob));

			// Act - Validate
			var isValid = _registry.IsValidJobType(customJobName);

			// Act - Retrieve
			var retrievedType = _registry.GetJobType(customJobName);

			// Assert
			isValid.Should().BeTrue();
			retrievedType.Should().Be(typeof(MarkTasksAsCompletedJob));
			_registry.GetAllJobTypes().Should().Contain(customJobName);
		}

		#endregion
	}
}
