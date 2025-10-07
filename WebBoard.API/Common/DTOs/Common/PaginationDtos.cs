namespace WebBoard.Common.DTOs.Common
{
    /// <summary>
    /// Request parameters for pagination, filtering, and sorting
    /// </summary>
    public class QueryParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page (max 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        /// <summary>
        /// Field to sort by
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort direction: asc or desc
        /// </summary>
        public string SortDirection { get; set; } = "desc";

        /// <summary>
        /// Search term for filtering
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Whether sorting is ascending
        /// </summary>
        public bool IsAscending => SortDirection?.ToLower() == "asc";
    }

    /// <summary>
    /// Paginated response wrapper with metadata
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = [];
        public PaginationMetadata Metadata { get; set; } = new();

        public PagedResult() { }

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

    /// <summary>
    /// Task-specific query parameters
    /// </summary>
    public class TaskQueryParameters : QueryParameters
    {
        /// <summary>
        /// Filter by task status (optional)
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Filter by whether task is assigned to a job
        /// </summary>
        public bool? HasJob { get; set; }

        public TaskQueryParameters()
        {
            // Default sort by creation date descending
            SortBy = "createdAt";
            SortDirection = "desc";
        }
    }

    /// <summary>
    /// Job-specific query parameters
    /// </summary>
    public class JobQueryParameters : QueryParameters
    {
        /// <summary>
        /// Filter by job status (optional)
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Filter by job type (optional)
        /// </summary>
        public string? JobType { get; set; }

        public JobQueryParameters()
        {
            // Default sort by creation date descending
            SortBy = "createdAt";
            SortDirection = "desc";
        }
    }
}
