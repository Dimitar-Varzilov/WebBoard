using FluentAssertions;
using WebBoard.API.Common.Attributes;

namespace WebBoard.Tests.Attributes
{
	public class NotInPastOffsetAttributeTests
	{
		[Fact]
		public void IsValid_WithFutureDate_ShouldReturnTrue()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute();
			var futureDate = DateTimeOffset.UtcNow.AddMinutes(5);

			// Act
			var result = attribute.IsValid(futureDate);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IsValid_WithPastDate_ShouldReturnFalse()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute();
			var pastDate = DateTimeOffset.UtcNow.AddMinutes(-5);

			// Act
			var result = attribute.IsValid(pastDate);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_WithDateExactlyNow_ShouldReturnFalse()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 1 };
			var now = DateTimeOffset.UtcNow;

			// Act
			var result = attribute.IsValid(now);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_WithNullValue_ShouldReturnTrue()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute();

			// Act
			var result = attribute.IsValid(null);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IsValid_WithNonDateTimeOffsetValue_ShouldReturnTrue()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute();

			// Act
			var result = attribute.IsValid("not a date");

			// Assert
			result.Should().BeTrue();
		}

		[Theory]
		[InlineData(1)]
		[InlineData(5)]
		[InlineData(10)]
		[InlineData(60)]
		public void IsValid_WithCustomMinimumMinutes_ShouldValidateCorrectly(int minimumMinutes)
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = minimumMinutes };
			var validDate = DateTimeOffset.UtcNow.AddMinutes(minimumMinutes + 1);
			var invalidDate = DateTimeOffset.UtcNow.AddMinutes(minimumMinutes - 1);

			// Act
			var validResult = attribute.IsValid(validDate);
			var invalidResult = attribute.IsValid(invalidDate);

			// Assert
			validResult.Should().BeTrue();
			invalidResult.Should().BeFalse();
		}

		[Fact]
		public void IsValid_WithDateExactlyAtMinimumThreshold_ShouldReturnTrue()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 5 };
			var thresholdDate = DateTimeOffset.UtcNow.AddMinutes(5).AddSeconds(1); // Slightly after threshold to account for execution time

			// Act
			var result = attribute.IsValid(thresholdDate);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void FormatErrorMessage_WithDefaultMinimum_ShouldReturnGenericMessage()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute();

			// Act
			var message = attribute.FormatErrorMessage("ScheduledAt");

			// Assert
			message.Should().Be("The ScheduledAt field cannot be in the past.");
		}

		[Fact]
		public void FormatErrorMessage_WithCustomMinimum_ShouldReturnSpecificMessage()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 30 };

			// Act
			var message = attribute.FormatErrorMessage("ScheduledAt");

			// Assert
			message.Should().Be("The ScheduledAt field must be at least 30 minutes in the future.");
		}

		[Fact]
		public void IsValid_WithUtcAndLocalTimezones_ShouldWorkCorrectly()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 1 };
			var utcFuture = DateTimeOffset.UtcNow.AddMinutes(5);
			var localFuture = DateTimeOffset.Now.AddMinutes(5);

			// Act
			var utcResult = attribute.IsValid(utcFuture);
			var localResult = attribute.IsValid(localFuture);

			// Assert
			utcResult.Should().BeTrue();
			localResult.Should().BeTrue();
		}

		[Fact]
		public void IsValid_WithDifferentTimezones_ShouldCompareCorrectly()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 1 };
			
			// Create a date in a different timezone but same instant
			var utcNow = DateTimeOffset.UtcNow;
			var easternTime = new DateTimeOffset(utcNow.DateTime, TimeSpan.FromHours(-5));
			var futureEastern = easternTime.AddMinutes(5);

			// Act
			var result = attribute.IsValid(futureEastern);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public void IsValid_WithZeroMinimumMinutes_ShouldRequireStrictlyFuture()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 0 };
			var future = DateTimeOffset.UtcNow.AddSeconds(1);

			// Act
			var futureResult = attribute.IsValid(future);

			// Assert
			futureResult.Should().BeTrue();
		}

		[Fact]
		public void IsValid_EdgeCase_JustUnderThreshold_ShouldReturnFalse()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 10 };
			var justUnder = DateTimeOffset.UtcNow.AddMinutes(10).AddSeconds(-1);

			// Act
			var result = attribute.IsValid(justUnder);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_EdgeCase_JustOverThreshold_ShouldReturnTrue()
		{
			// Arrange
			var attribute = new NotInPastOffsetAttribute { MinimumMinutesInFuture = 10 };
			var justOver = DateTimeOffset.UtcNow.AddMinutes(10).AddSeconds(1);

			// Act
			var result = attribute.IsValid(justOver);

			// Assert
			result.Should().BeTrue();
		}
	}
}
