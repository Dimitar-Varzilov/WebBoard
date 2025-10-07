namespace WebBoard.API.Services.Jobs
{
	/// <summary>
	/// Attribute to mark a job class and specify its string identifier
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class JobTypeAttribute(string jobTypeName) : Attribute
	{
		public string JobTypeName { get; } = jobTypeName ?? throw new ArgumentNullException(nameof(jobTypeName));
	}
}