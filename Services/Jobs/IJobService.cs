using WebBoard.Common.DTOs.Common;
using WebBoard.Common.DTOs.Jobs;

namespace WebBoard.Services.Jobs
{
	public interface IJobService
	{
		Task<PagedResult<JobDto>> GetJobsAsync(JobQueryParameters parameters);
		Task<JobDto?> GetJobByIdAsync(Guid id);
		Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest);
	}
}
