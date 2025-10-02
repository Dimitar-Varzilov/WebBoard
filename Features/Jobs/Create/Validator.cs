using FastEndpoints;
using FluentValidation;
using WebBoard.Common;

namespace WebBoard.Features.Jobs.Create
{
	public class CreateJobValidator : Validator<CreateJobRequest>
	{
		public CreateJobValidator()
		{
			RuleFor(x => x.JobType)
				.NotEmpty().WithMessage("Job type is required")
				.Must(BeValidJobType).WithMessage($"Job type must be either '{Constants.JobTypes.MarkTasksAsCompleted}' or '{Constants.JobTypes.GenerateTaskList}'");
		}

		private static bool BeValidJobType(string jobType)
		{
			return jobType is Constants.JobTypes.MarkTasksAsCompleted or Constants.JobTypes.GenerateTaskList;
		}
	}
}
