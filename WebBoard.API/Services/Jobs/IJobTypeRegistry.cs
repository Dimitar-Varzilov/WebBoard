namespace WebBoard.API.Services.Jobs
{
	public interface IJobTypeRegistry
	{
		Type GetJobType(string jobTypeName);
		bool IsValidJobType(string jobTypeName);
		IEnumerable<string> GetAllJobTypes();
	}
}