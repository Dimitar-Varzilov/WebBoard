using WebBoard.API.Common.Models;

namespace WebBoard.API.Services.Jobs
{
	public interface IJobSchedulingService
	{
		Task ScheduleJobAsync(Job job);
		Task RescheduleJobAsync(Job job);
	}
}