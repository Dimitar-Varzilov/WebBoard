using System.ComponentModel.DataAnnotations;

namespace WebBoard.API.Common.Models
{
	public class RefreshToken
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		public string UserId { get; set; } = string.Empty;

		[Required]
		public string TokenHash { get; set; } = string.Empty;

		public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

		public DateTime ExpiresUtc { get; set; }

		public DateTime? RevokedUtc { get; set; }

		// When rotating, store the token value (hashed) that replaced this one
		public string? ReplacedByTokenHash { get; set; }

		public bool IsActive => RevokedUtc == null && DateTime.UtcNow <= ExpiresUtc;
	}
}
