using Sieve.Models;

namespace WebBoard.API.Common.DTOs.Common
{
	/// <summary>
	/// Request parameters for pagination, filtering, and sorting
	/// </summary>
	public class QueryParameters : SieveModel
	{
		
	}

	/// <summary>
	/// Paginated response wrapper with metadata
	/// </summary>
	public class PagedResult<T>
	{
		public IEnumerable<T> Items { get; set; } = [];
		public PaginationMetadata Metadata { get; set; } = new();

		public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
		{
			Items = items;
			Metadata = new PaginationMetadata
			{
				TotalCount = totalCount,
				PageSize = pageSize,
				CurrentPage = pageNumber,
				TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
			};
		}
	}

	/// <summary>
	/// Pagination metadata
	/// </summary>
	public class PaginationMetadata
	{
		/// <summary>
		/// Total number of items across all pages
		/// </summary>
		public int TotalCount { get; set; }

		/// <summary>
		/// Number of items per page
		/// </summary>
		public int PageSize { get; set; }

		/// <summary>
		/// Current page number (1-based)
		/// </summary>
		public int CurrentPage { get; set; }

		/// <summary>
		/// Total number of pages
		/// </summary>
		public int TotalPages { get; set; }

		/// <summary>
		/// Whether there is a previous page
		/// </summary>
		public bool HasPrevious => CurrentPage > 1;

		/// <summary>
		/// Whether there is a next page
		/// </summary>
		public bool HasNext => CurrentPage < TotalPages;
	}

}
