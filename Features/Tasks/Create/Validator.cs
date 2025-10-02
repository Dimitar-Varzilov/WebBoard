using FastEndpoints;
using FluentValidation;

namespace WebBoard.Features.Tasks.Create
{
	public class CreateTaskValidator : Validator<CreateTaskRequest>
	{
		public CreateTaskValidator()
		{
			RuleFor(x => x.Title)
				.NotEmpty().WithMessage("Заглавието е задължително")
				.MaximumLength(200).WithMessage("Заглавието не може да е повече от 200 символа");

			RuleFor(x => x.Description)
				.MaximumLength(1000);
		}
	}

}
