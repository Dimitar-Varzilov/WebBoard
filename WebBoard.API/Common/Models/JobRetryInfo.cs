namespace WebBoard.API.Common.Models
{
	/// <summary>
	/// Tracks retry attempts for failed jobs
	/// </summary>
	public record JobRetryInfo(
		Guid Id,
		Guid JobId,
		int RetryCount,
		int MaxRetries,
		DateTimeOffset NextRetryAt,
		string? LastErrorMessage,
		DateTimeOffset CreatedAt)
	{
		/// <summary>
		/// Navigation property to the associated job
		/// </summary>
		public Job? Job { get; init; }
	}
}
