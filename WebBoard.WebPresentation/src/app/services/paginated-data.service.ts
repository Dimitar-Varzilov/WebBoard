import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  switchMap,
  tap,
  catchError,
} from 'rxjs/operators';
import {
  PagedResult,
  QueryParameters,
  PaginationMetadata,
  DEFAULT_PAGE_SIZE,
} from '../models';

/**
 * Generic state for paginated data
 */
export interface PaginatedState<TParams extends QueryParameters> {
  data: any[];
  metadata: PaginationMetadata | null;
  loading: boolean;
  error: string | null;
  parameters: TParams;
}

/**
 * Generic service for managing paginated data with filtering and sorting
 * Handles state management, debouncing, and data fetching
 *
 * Note: This service is not injected directly. Use the PaginationFactory
 * service to create instances for any entity type.
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
export class PaginatedDataService<T, TParams extends QueryParameters> {
  private stateSubject: BehaviorSubject<PaginatedState<TParams>>;
  private refreshSubject = new Subject<void>();

  public state$: Observable<PaginatedState<TParams>>;

  // Expose individual observables for convenience
  public data$: Observable<T[]>;
  public metadata$: Observable<PaginationMetadata | null>;
  public loading$: Observable<boolean>;
  public error$: Observable<string | null>;

  constructor(
    private fetchFn: (params: TParams) => Observable<PagedResult<T>>,
    private defaultParams: TParams
  ) {
    const initialState: PaginatedState<TParams> = {
      data: [],
      metadata: null,
      loading: false,
      error: null,
      parameters: { ...defaultParams },
    };

    this.stateSubject = new BehaviorSubject<PaginatedState<TParams>>(
      initialState
    );
    this.state$ = this.stateSubject.asObservable();

    // Derived observables
    this.data$ = new Observable((observer) => {
      this.state$.subscribe((state) => observer.next(state.data as T[]));
    });

    this.metadata$ = new Observable((observer) => {
      this.state$.subscribe((state) => observer.next(state.metadata));
    });

    this.loading$ = new Observable((observer) => {
      this.state$.subscribe((state) => observer.next(state.loading));
    });

    this.error$ = new Observable((observer) => {
      this.state$.subscribe((state) => observer.next(state.error));
    });

    // Setup auto-refresh on parameter changes with debounce
    this.refreshSubject
      .pipe(
        debounceTime(300), // Debounce search input
        distinctUntilChanged(),
        tap(() => this.setLoading(true)),
        switchMap(() => this.fetchData())
      )
      .subscribe({
        error: (error) => {
          // Error is already handled in fetchData's tap operator
          // This prevents uncaught errors from propagating
          console.error('Error in refresh subscription:', error);
        },
      });
  }

  /**
   * Get current state snapshot
   */
  get currentState(): PaginatedState<TParams> {
    return this.stateSubject.value;
  }

  /**
   * Get current data
   */
  get data(): T[] {
    return this.currentState.data as T[];
  }

  /**
   * Get current data (method form for consistency)
   */
  getData(): T[] {
    return this.data;
  }

  /**
   * Get current metadata
   */
  get metadata(): PaginationMetadata | null {
    return this.currentState.metadata;
  }

  /**
   * Get current metadata (method form for consistency)
   */
  getMetadata(): PaginationMetadata | null {
    return this.metadata;
  }

  /**
   * Get current parameters
   */
  get parameters(): TParams {
    return this.currentState.parameters;
  }

  /**
   * Get current parameters (method form for consistency)
   */
  getCurrentParams(): TParams {
    return this.parameters;
  }

  /**
   * Check if currently loading
   */
  isLoading(): boolean {
    return this.currentState.loading;
  }

  /**
   * Get current error
   */
  getError(): string | null {
    return this.currentState.error;
  }

  /**
   * Load data with current parameters
   */
  load(): void {
    this.refreshSubject.next();
  }

  /**
   * Reload data (bypass debounce)
   */
  reload(): void {
    this.setLoading(true);
    this.fetchData().subscribe();
  }

  /**
   * Update query parameters and trigger reload
   */
  updateParameters(params: Partial<TParams>, reload = true): void {
    const newParams = { ...this.currentState.parameters, ...params };
    this.setState({ parameters: newParams });

    if (reload) {
      this.load();
    }
  }

  /**
   * Alias for updateParameters (shorter name)
   */
  updateParams(params: Partial<TParams>, reload = true): void {
    this.updateParameters(params, reload);
  }

  /**
   * Refresh current data
   */
  refresh(): void {
    this.reload();
  }

  /**
   * Set search term
   */
  setSearchTerm(searchTerm: string): void {
    this.updateParameters({ searchTerm, pageNumber: 1 } as Partial<TParams>);
  }

  /**
   * Set page number
   */
  setPage(pageNumber: number): void {
    this.updateParameters({ pageNumber } as Partial<TParams>);
  }

  /**
   * Set page size
   */
  setPageSize(pageSize: number): void {
    this.updateParameters({ pageSize, pageNumber: 1 } as Partial<TParams>);
  }

  /**
   * Set sort column and direction
   */
  setSort(sortBy: string, sortDirection: 'asc' | 'desc' = 'desc'): void {
    this.updateParameters({ sortBy, sortDirection } as Partial<TParams>);
  }

  /**
   * Toggle sort direction for a column
   */
  toggleSort(sortBy: string): void {
    const currentParams = this.currentState.parameters;
    const newDirection =
      currentParams.sortBy === sortBy && currentParams.sortDirection === 'asc'
        ? 'desc'
        : 'asc';
    this.setSort(sortBy, newDirection);
  }

  /**
   * Reset to default parameters
   */
  reset(): void {
    this.setState({ parameters: { ...this.defaultParams } });
    this.load();
  }

  /**
   * Clear search and filters
   */
  clearFilters(): void {
    const clearedParams = {
      ...this.currentState.parameters,
      searchTerm: undefined,
      pageNumber: 1,
    };
    this.setState({ parameters: clearedParams as TParams });
    this.load();
  }

  /**
   * Private: Fetch data from API
   */
  private fetchData(): Observable<PagedResult<T>> {
    return this.fetchFn(this.currentState.parameters).pipe(
      tap({
        next: (result) => {
          this.setState({
            data: result.items,
            metadata: result.metadata,
            loading: false,
            error: null,
          });
        },
      }),
      catchError((error) => {
        console.error('Error fetching paginated data:', error);
        this.setState({
          data: [],
          metadata: null,
          loading: false,
          error: error.message || 'Failed to load data',
        });
        // Return empty result to prevent error propagation
        return of({
          items: [],
          metadata: {
            currentPage: 1,
            pageSize: this.currentState.parameters.pageSize || 10,
            totalCount: 0,
            totalPages: 0,
            hasPrevious: false,
            hasNext: false,
          },
        });
      })
    );
  }

  /**
   * Private: Update state
   */
  private setState(partial: Partial<PaginatedState<TParams>>): void {
    this.stateSubject.next({ ...this.currentState, ...partial });
  }

  /**
   * Private: Set loading state
   */
  private setLoading(loading: boolean): void {
    this.setState({ loading });
  }
}
