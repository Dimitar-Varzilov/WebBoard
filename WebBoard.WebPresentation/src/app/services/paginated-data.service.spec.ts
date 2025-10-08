import { PaginatedDataService } from './paginated-data.service';
import { Observable, of, throwError } from 'rxjs';
import { PagedResult, QueryParameters, PaginationMetadata } from '../models';

interface TestItem {
  id: string;
  name: string;
}

interface TestQueryParams extends QueryParameters {
  status?: string;
}

const createMockMetadata = (
  overrides?: Partial<PaginationMetadata>
): PaginationMetadata => ({
  currentPage: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0,
  hasPrevious: false,
  hasNext: false,
  ...overrides,
});

describe('PaginatedDataService', () => {
  let service: PaginatedDataService<TestItem, TestQueryParams>;
  let mockFetchFn: jasmine.Spy<
    (params: TestQueryParams) => Observable<PagedResult<TestItem>>
  >;

  const mockData: TestItem[] = [
    { id: '1', name: 'Item 1' },
    { id: '2', name: 'Item 2' },
  ];

  const mockPagedResult: PagedResult<TestItem> = {
    items: mockData,
    metadata: createMockMetadata({
      totalCount: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    }),
  };

  beforeEach(() => {
    mockFetchFn = jasmine
      .createSpy('fetchFn')
      .and.returnValue(of(mockPagedResult));

    const defaultParams: TestQueryParams = {
      pageNumber: 1,
      pageSize: 10,
      sortDirection: 'desc',
    };

    service = new PaginatedDataService<TestItem, TestQueryParams>(
      mockFetchFn,
      defaultParams
    );
  });

  afterEach((done) => {
    // Wait for any pending debounced operations to complete
    setTimeout(() => {
      done();
    }, 350); // Slightly longer than the 300ms debounce time
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('initialization', () => {
    it('should initialize with default parameters', () => {
      expect(service.parameters.pageNumber).toBe(1);
      expect(service.parameters.pageSize).toBe(10);
      expect(service.parameters.sortDirection).toBe('desc');
    });

    it('should have empty data initially', () => {
      expect(service.data).toEqual([]);
      expect(service.metadata).toBeNull();
      expect(service.isLoading()).toBe(false);
      expect(service.getError()).toBeNull();
    });
  });

  describe('load', () => {
    it('should load data successfully', (done) => {
      service.load();

      setTimeout(() => {
        expect(mockFetchFn).toHaveBeenCalled();
        expect(service.data).toEqual(mockData);
        expect(service.metadata?.totalCount).toBe(2);
        expect(service.isLoading()).toBe(false);
        done();
      }, 400);
    });

    it('should trigger fetch for each load call', (done) => {
      service.load();
      service.load();
      service.load();

      setTimeout(() => {
        expect(mockFetchFn).toHaveBeenCalledTimes(3);
        done();
      }, 100);
    });
  });

  describe('reload', () => {
    it('should reload data without debounce', (done) => {
      service.reload();

      setTimeout(() => {
        expect(mockFetchFn).toHaveBeenCalled();
        expect(service.data).toEqual(mockData);
        done();
      }, 100);
    });
  });

  describe('updateParameters', () => {
    it('should update parameters and trigger reload', (done) => {
      service.updateParameters({ pageSize: 25 });

      setTimeout(() => {
        expect(service.parameters.pageSize).toBe(25);
        expect(mockFetchFn).toHaveBeenCalled();
        done();
      }, 400);
    });

    it('should not reload when reload parameter is false', () => {
      service.updateParameters({ pageSize: 25 }, false);
      expect(service.parameters.pageSize).toBe(25);
      expect(mockFetchFn).not.toHaveBeenCalled();
    });

    it('should merge new parameters with existing ones', (done) => {
      service.updateParameters({ status: 'active' });

      setTimeout(() => {
        expect(service.parameters.pageNumber).toBe(1);
        expect(service.parameters.pageSize).toBe(10);
        expect((service.parameters as TestQueryParams).status).toBe('active');
        done();
      }, 400);
    });
  });

  describe('setSearchTerm', () => {
    it('should set search term and reset to page 1', (done) => {
      service.updateParameters({ pageNumber: 3 }, false);
      service.setSearchTerm('test search');

      setTimeout(() => {
        expect(service.parameters.searchTerm).toBe('test search');
        expect(service.parameters.pageNumber).toBe(1);
        done();
      }, 400);
    });
  });

  describe('setPage', () => {
    it('should set page number', (done) => {
      service.setPage(3);

      setTimeout(() => {
        expect(service.parameters.pageNumber).toBe(3);
        expect(mockFetchFn).toHaveBeenCalled();
        done();
      }, 400);
    });
  });

  describe('setPageSize', () => {
    it('should set page size and reset to page 1', (done) => {
      service.updateParameters({ pageNumber: 3 }, false);
      service.setPageSize(50);

      setTimeout(() => {
        expect(service.parameters.pageSize).toBe(50);
        expect(service.parameters.pageNumber).toBe(1);
        done();
      }, 400);
    });
  });

  describe('setSort', () => {
    it('should set sort column and direction', (done) => {
      service.setSort('name', 'asc');

      setTimeout(() => {
        expect(service.parameters.sortBy).toBe('name');
        expect(service.parameters.sortDirection).toBe('asc');
        done();
      }, 400);
    });

    it('should default to desc direction', (done) => {
      service.setSort('createdAt');

      setTimeout(() => {
        expect(service.parameters.sortBy).toBe('createdAt');
        expect(service.parameters.sortDirection).toBe('desc');
        done();
      }, 400);
    });
  });

  describe('toggleSort', () => {
    it('should toggle sort direction for same column', (done) => {
      service.updateParameters({ sortBy: 'name', sortDirection: 'asc' }, false);
      service.toggleSort('name');

      setTimeout(() => {
        expect(service.parameters.sortBy).toBe('name');
        expect(service.parameters.sortDirection).toBe('desc');
        done();
      }, 400);
    });

    it('should set to asc when changing column', (done) => {
      service.updateParameters(
        { sortBy: 'name', sortDirection: 'desc' },
        false
      );
      service.toggleSort('createdAt');

      setTimeout(() => {
        expect(service.parameters.sortBy).toBe('createdAt');
        expect(service.parameters.sortDirection).toBe('asc');
        done();
      }, 400);
    });
  });

  describe('reset', () => {
    it('should reset to default parameters', (done) => {
      service.updateParameters(
        { pageNumber: 3, pageSize: 50, searchTerm: 'test' },
        false
      );

      service.reset();

      setTimeout(() => {
        expect(service.parameters.pageNumber).toBe(1);
        expect(service.parameters.pageSize).toBe(10);
        expect(service.parameters.searchTerm).toBeUndefined();
        expect(mockFetchFn).toHaveBeenCalled();
        done();
      }, 400);
    });
  });

  describe('clearFilters', () => {
    it('should clear search term and reset to page 1', (done) => {
      service.updateParameters(
        {
          pageNumber: 3,
          searchTerm: 'test',
          status: 'active',
        } as Partial<TestQueryParams>,
        false
      );

      service.clearFilters();

      setTimeout(() => {
        expect(service.parameters.searchTerm).toBeUndefined();
        expect(service.parameters.pageNumber).toBe(1);
        done();
      }, 400);
    });
  });

  describe('error handling', () => {
    it('should handle fetch errors', (done) => {
      const errorMessage = 'Failed to fetch data';

      // Create a new service instance for this test to avoid contaminating other tests
      const errorFetchFn = jasmine
        .createSpy('errorFetchFn')
        .and.returnValue(throwError(() => new Error(errorMessage)));

      const defaultParams: TestQueryParams = {
        pageNumber: 1,
        pageSize: 10,
        sortDirection: 'desc',
      };

      const errorService = new PaginatedDataService<TestItem, TestQueryParams>(
        errorFetchFn,
        defaultParams
      );

      // Spy on console.error to suppress error output in test
      const consoleSpy = spyOn(console, 'error');

      // Wait for the error to propagate through the observable chain
      setTimeout(() => {
        expect(errorService.data).toEqual([]);
        expect(errorService.metadata).toBeNull();
        expect(errorService.getError()).toBe(errorMessage);
        expect(errorService.isLoading()).toBe(false);
        expect(consoleSpy).toHaveBeenCalledWith(
          'Error fetching paginated data:',
          jasmine.any(Error)
        );
        done();
      }, 150);

      errorService.reload();
    });
  });
  describe('observables', () => {
    it('should emit data updates', (done) => {
      service.data$.subscribe((data) => {
        if (data.length > 0) {
          expect(data).toEqual(mockData);
          done();
        }
      });

      service.reload();
    });

    it('should emit metadata updates', (done) => {
      service.metadata$.subscribe((metadata) => {
        if (metadata) {
          expect(metadata.totalCount).toBe(2);
          done();
        }
      });

      service.reload();
    });

    it('should emit loading state changes', (done) => {
      let loadingStates: boolean[] = [];
      service.loading$.subscribe((loading) => {
        loadingStates.push(loading);
        if (loadingStates.length === 2) {
          expect(loadingStates).toContain(true);
          expect(loadingStates).toContain(false);
          done();
        }
      });

      service.reload();
    });
  });

  describe('getters', () => {
    it('should return current data via getData()', (done) => {
      service.reload();

      setTimeout(() => {
        expect(service.getData()).toEqual(mockData);
        done();
      }, 100);
    });

    it('should return current metadata via getMetadata()', (done) => {
      service.reload();

      setTimeout(() => {
        const metadata = service.getMetadata();
        expect(metadata).toBeTruthy();
        expect(metadata?.totalCount).toBe(2);
        done();
      }, 100);
    });

    it('should return current parameters via getCurrentParams()', () => {
      expect(service.getCurrentParams()).toBeTruthy();
      expect(service.getCurrentParams().pageNumber).toBe(1);
    });
  });
});
