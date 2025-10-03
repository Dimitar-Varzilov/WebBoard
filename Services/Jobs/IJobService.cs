using WebBoard.Common.DTOs.Jobs;

namespace WebBoard.Services.Jobs
{
	public interface IJobService
	{
		Task<IEnumerable<JobDto>> GetAllJobsAsync();
		Task<JobDto?> GetJobByIdAsync(Guid id);
		Task<JobDto> CreateJobAsync(CreateJobRequestDto createJobRequest);
	}
}
