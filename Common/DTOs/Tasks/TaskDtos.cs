using System.ComponentModel.DataAnnotations;
using WebBoard.Common.Enums;

namespace WebBoard.Common.DTOs.Tasks
{
	public record TaskDto(Guid Id, string Title, string Description, TaskItemStatus Status, DateTime CreatedAt);

	public record CreateTaskRequestDto(
		[Required]
		[MinLength(3)]
		string Title,
		[Required]
		string Description);

	public record UpdateTaskRequestDto(
		[Required]
		[MinLength(3)]
		string Title,
		[Required]
		string Description,
		[Required]
		TaskItemStatus Status);
}
