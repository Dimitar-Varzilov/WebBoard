using WebBoard.Common.Models;

namespace WebBoard.Services.Jobs
{
	public interface IJobSchedulingService
	{
		Task ScheduleJobAsync(Job job);
	}
}