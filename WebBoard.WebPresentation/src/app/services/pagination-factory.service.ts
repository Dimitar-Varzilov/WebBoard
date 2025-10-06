import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PaginatedDataService } from './paginated-data.service';
import {
  QueryParameters,
  PagedResult,
  DEFAULT_PAGE_SIZE,
  DEFAULT_PAGE_NUMBER,
  DEFAULT_SORT_DIRECTION,
} from '../models';

/**
 * Generic factory for creating PaginatedDataService instances
 * This service eliminates the need for separate factory services for each entity type
 *
 * @example
 * // In a component:
 * constructor(private paginationFactory: PaginationFactory) {}
 *
 * ngOnInit() {
 *   this.paginationService = this.paginationFactory.create<TaskDto, TaskQueryParameters>(
 *     (params) => this.taskService.getTasks(params),
 *     { pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }
 *   );
 * }
 */
@Injectable({
  providedIn: 'root',
})
export class PaginationFactory {
  /**
   * Create a new paginated data service instance
   *
   * @template T - The type of items in the paginated result
   * @template TParams - The type of query parameters (must extend QueryParameters)
   * @param fetchFn - Function that fetches paginated data from the API
   * @param initialParams - Initial query parameters (optional)
   * @returns A new PaginatedDataService instance
   */
  create<T, TParams extends QueryParameters>(
    fetchFn: (params: TParams) => Observable<PagedResult<T>>,
    initialParams?: Partial<TParams>
  ): PaginatedDataService<T, TParams> {
    const defaultParams: QueryParameters = {
      pageNumber: DEFAULT_PAGE_NUMBER,
      pageSize: DEFAULT_PAGE_SIZE,
      sortDirection: DEFAULT_SORT_DIRECTION,
      ...initialParams,
    };

    return new PaginatedDataService<T, TParams>(
      fetchFn,
      defaultParams as TParams
    );
  }
}
