using FastEndpoints;
using FluentValidation;

namespace WebBoard.Features.Jobs.Create
{
	public class CreateJobValidator : Validator<CreateJobRequest>
	{
		public CreateJobValidator()
		{
			RuleFor(x => x.JobType)
				.NotEmpty().WithMessage("Job type is required")
				.Must(BeValidJobType).WithMessage("Job type must be either 'MarkTasksAsCompleted' or 'GenerateTaskList'");
		}

		private static bool BeValidJobType(string jobType)
		{
			return jobType is "MarkTasksAsCompleted" or "GenerateTaskList";
		}
	}
}
