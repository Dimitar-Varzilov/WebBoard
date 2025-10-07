import { TestBed } from '@angular/core/testing';
import { PaginationFactory } from './pagination-factory.service';
import { PaginatedDataService } from './paginated-data.service';
import { Observable, of } from 'rxjs';
import {
  PagedResult,
  QueryParameters,
  PaginationMetadata,
  DEFAULT_PAGE_SIZE,
  DEFAULT_PAGE_NUMBER,
  DEFAULT_SORT_DIRECTION,
} from '../models';

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

describe('PaginationFactory', () => {
  let factory: PaginationFactory;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PaginationFactory],
    });
    factory = TestBed.inject(PaginationFactory);
  });

  it('should be created', () => {
    expect(factory).toBeTruthy();
  });

  describe('create', () => {
    it('should create a PaginatedDataService instance', () => {
      const mockFetchFn = (
        params: TestQueryParams
      ): Observable<PagedResult<TestItem>> =>
        of({
          items: [],
          metadata: createMockMetadata(),
        });

      const service = factory.create<TestItem, TestQueryParams>(mockFetchFn);

      expect(service).toBeInstanceOf(PaginatedDataService);
    });

    it('should create service with default parameters', () => {
      const mockFetchFn = (
        params: TestQueryParams
      ): Observable<PagedResult<TestItem>> =>
        of({
          items: [],
          metadata: createMockMetadata(),
        });

      const service = factory.create<TestItem, TestQueryParams>(mockFetchFn);

      expect(service.parameters.pageNumber).toBe(DEFAULT_PAGE_NUMBER);
      expect(service.parameters.pageSize).toBe(DEFAULT_PAGE_SIZE);
      expect(service.parameters.sortDirection).toBe(DEFAULT_SORT_DIRECTION);
    });

    it('should create service with custom initial parameters', () => {
      const mockFetchFn = (
        params: TestQueryParams
      ): Observable<PagedResult<TestItem>> =>
        of({
          items: [],
          metadata: createMockMetadata({ pageSize: 25 }),
        });

      const initialParams: Partial<TestQueryParams> = {
        pageSize: 25,
        sortBy: 'name',
        sortDirection: 'asc',
        status: 'active',
      };

      const service = factory.create<TestItem, TestQueryParams>(
        mockFetchFn,
        initialParams
      );

      expect(service.parameters.pageSize).toBe(25);
      expect(service.parameters.sortBy).toBe('name');
      expect(service.parameters.sortDirection).toBe('asc');
      expect((service.parameters as TestQueryParams).status).toBe('active');
    });

    it('should merge initial params with defaults', () => {
      const mockFetchFn = (
        params: TestQueryParams
      ): Observable<PagedResult<TestItem>> =>
        of({
          items: [],
          metadata: createMockMetadata(),
        });

      const initialParams: Partial<TestQueryParams> = {
        sortBy: 'createdAt',
      };

      const service = factory.create<TestItem, TestQueryParams>(
        mockFetchFn,
        initialParams
      );

      // Should have custom sortBy
      expect(service.parameters.sortBy).toBe('createdAt');
      // Should have default pageNumber
      expect(service.parameters.pageNumber).toBe(DEFAULT_PAGE_NUMBER);
      // Should have default pageSize
      expect(service.parameters.pageSize).toBe(DEFAULT_PAGE_SIZE);
    });

    it('should create independent service instances', () => {
      const mockFetchFn1 = (
        params: TestQueryParams
      ): Observable<PagedResult<TestItem>> =>
        of({
          items: [{ id: '1', name: 'Test 1' }],
          metadata: createMockMetadata({
            pageSize: 10,
            totalCount: 1,
            totalPages: 1,
          }),
        });

      const mockFetchFn2 = (
        params: TestQueryParams
      ): Observable<PagedResult<TestItem>> =>
        of({
          items: [{ id: '2', name: 'Test 2' }],
          metadata: createMockMetadata({
            pageSize: 20,
            totalCount: 1,
            totalPages: 1,
          }),
        });

      const service1 = factory.create<TestItem, TestQueryParams>(mockFetchFn1, {
        pageSize: 10,
      });
      const service2 = factory.create<TestItem, TestQueryParams>(mockFetchFn2, {
        pageSize: 20,
      });

      expect(service1).not.toBe(service2);
      expect(service1.parameters.pageSize).toBe(10);
      expect(service2.parameters.pageSize).toBe(20);
    });

    it('should pass fetch function to service', (done) => {
      const mockData: TestItem[] = [
        { id: '1', name: 'Item 1' },
        { id: '2', name: 'Item 2' },
      ];

      const mockFetchFn = jasmine.createSpy('fetchFn').and.returnValue(
        of({
          items: mockData,
          metadata: {
            currentPage: 1,
            pageSize: 10,
            totalCount: 2,
            totalPages: 1,
          },
        })
      );

      const service = factory.create<TestItem, TestQueryParams>(mockFetchFn);
      service.reload();

      // Wait for async operation
      setTimeout(() => {
        expect(mockFetchFn).toHaveBeenCalled();
        done();
      }, 400);
    });
  });

  describe('factory pattern usage', () => {
    it('should work with different entity types', () => {
      interface TaskItem {
        id: string;
        title: string;
        status: number;
      }

      interface JobItem {
        id: string;
        jobType: string;
      }

      const taskFetchFn = (
        params: QueryParameters
      ): Observable<PagedResult<TaskItem>> =>
        of({
          items: [],
          metadata: createMockMetadata(),
        });

      const jobFetchFn = (
        params: QueryParameters
      ): Observable<PagedResult<JobItem>> =>
        of({
          items: [],
          metadata: createMockMetadata(),
        });

      const taskService = factory.create<TaskItem, QueryParameters>(
        taskFetchFn
      );
      const jobService = factory.create<JobItem, QueryParameters>(jobFetchFn);

      expect(taskService).toBeTruthy();
      expect(jobService).toBeTruthy();
      // Services are independent instances
      expect(taskService !== (jobService as any)).toBe(true);
    });
  });
});
