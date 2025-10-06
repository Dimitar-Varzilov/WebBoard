/**
 * Pagination metadata
 */
export interface PaginationMetadata {
  totalCount: number;
  pageSize: number;
  currentPage: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/**
 * Paginated response wrapper
 */
export interface PagedResult<T> {
  items: T[];
  metadata: PaginationMetadata;
}

/**
 * Query parameters for pagination, filtering, and sorting
 */
export interface QueryParameters {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  searchTerm?: string;
}

/**
 * Task-specific query parameters
 */
export interface TaskQueryParameters extends QueryParameters {
  status?: number;
  hasJob?: boolean;
}

/**
 * Job-specific query parameters
 */
export interface JobQueryParameters extends QueryParameters {
  status?: number;
  jobType?: string;
}

/**
 * Default page sizes
 */
export const PAGE_SIZES = [10, 25, 50, 100];

/**
 * Default query parameters
 */
export const DEFAULT_PAGE_SIZE = 10;
export const DEFAULT_PAGE_NUMBER = 1;
export const DEFAULT_SORT_DIRECTION = 'desc';
