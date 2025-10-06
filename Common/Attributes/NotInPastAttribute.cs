using System.ComponentModel.DataAnnotations;

namespace WebBoard.Common.Attributes
{
	public class NotInPastAttribute : ValidationAttribute
	{
		public override bool IsValid(object? value)
		{
			if (value is DateTime dateTime)
			{
				return dateTime > DateTime.UtcNow;
			}
			
			// If value is null or not DateTime, let other validators handle it
			return true;
		}

		public override string FormatErrorMessage(string name)
		{
			return $"The {name} field cannot be in the past.";
		}
	}
}