using System.ComponentModel.DataAnnotations;

namespace WebBoard.Common.Attributes
{
	/// <summary>
	/// Validation attribute for DateTimeOffset values that cannot be in the past
	/// Preferred over DateTime for timezone-aware applications
	/// </summary>
	public class NotInPastOffsetAttribute : ValidationAttribute
	{
		/// <summary>
		/// Minimum minutes in the future (default: 1 minute buffer)
		/// </summary>
		public int MinimumMinutesInFuture { get; set; } = 1;

		public override bool IsValid(object? value)
		{
			if (value is DateTimeOffset dateTimeOffset)
			{
				var minimumTime = DateTimeOffset.UtcNow.AddMinutes(MinimumMinutesInFuture);
				return dateTimeOffset >= minimumTime;
			}
			
			// If value is null or not DateTimeOffset, let other validators handle it
			return true;
		}

		public override string FormatErrorMessage(string name)
		{
			if (MinimumMinutesInFuture <= 1)
			{
				return $"The {name} field cannot be in the past.";
			}
			return $"The {name} field must be at least {MinimumMinutesInFuture} minutes in the future.";
		}
	}
}