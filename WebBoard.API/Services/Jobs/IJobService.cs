using WebBoard.API.Common.DTOs.Common;
using WebBoard.API.Common.DTOs.Jobs;

namespace WebBoard.API.Services.Jobs
{
	public interface IJobService
	{
		Task<PagedResult<JobDto>> GetJobsAsync(JobQueryParameters parameters);
		Task<JobDto?> GetJobByIdAsync(Guid id);
		Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest);
		Task<JobDto?> UpdateJobAsync(Guid id, UpdateJobRequestDto updateJobRequest);
	}
}
