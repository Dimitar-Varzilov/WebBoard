using System.ComponentModel.DataAnnotations;
using WebBoard.Common.Enums;

namespace WebBoard.Common.DTOs.Tasks
{
	public record TaskDto(Guid Id, string Title, string Description, TaskItemStatus Status, DateTime CreatedAt);

	public record CreateTaskRequestDto(
		[Required]
		[MinLength(3, ErrorMessage = "Title must be at least 3 characters long")]
		string Title,
		[Required]
		[MinLength(3, ErrorMessage = "Description must be at least 3 characters long")]
		string Description);

	public record UpdateTaskRequestDto(
		[Required]
		[MinLength(3, ErrorMessage = "Title must be at least 3 characters long")]
		string Title,
		[Required]
		[MinLength(3, ErrorMessage = "Description must be at least 3 characters long")]
		string Description,
		[Required]
		TaskItemStatus Status);
}
