using WebBoard.API.Common.DTOs.Common;

namespace WebBoard.API.Common.DTOs.Tasks
{
	public class TaskQueryParameters : QueryParameters
	{
		public int? Status { get; set; }
		public bool? HasJob { get; set; }
	}
}