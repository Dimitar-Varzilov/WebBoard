import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TaskListComponent } from './task-list.component';
import { TaskService } from '../../../services';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { PaginationFactory } from '../../../services/pagination-factory.service';
import { PaginatedDataService } from '../../../services/paginated-data.service';
import { TaskDto, TaskItemStatus, DEFAULT_PAGE_SIZE } from '../../../models';
import { ROUTES } from '../../../constants';
import { of, throwError, BehaviorSubject } from 'rxjs';

describe('TaskListComponent', () => {
  let component: TaskListComponent;
  let fixture: ComponentFixture<TaskListComponent>;
  let mockTaskService: jasmine.SpyObj<TaskService>;
  let mockPaginationFactory: jasmine.SpyObj<PaginationFactory>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockPaginationService: jasmine.SpyObj<PaginatedDataService<any, any>>;

  const mockTasks: TaskDto[] = [
    {
      id: 'task-1',
      title: 'Test Task 1',
      description: 'Description 1',
      status: TaskItemStatus.Pending,
      createdAt: '2024-01-01T00:00:00Z',
      createdAtDate: new Date('2024-01-01'),
      createdAtDisplay: 'Jan 1, 2024',
      createdAtRelative: '1 day ago',
      createdAtCompact: '1/1/24',
      isRecent: false,
      age: 1,
      isAssignedToJob: false,
    },
    {
      id: 'task-2',
      title: 'Important Task 2',
      description: 'Description 2',
      status: TaskItemStatus.InProgress,
      createdAt: '2024-01-02T00:00:00Z',
      createdAtDate: new Date('2024-01-02'),
      createdAtDisplay: 'Jan 2, 2024',
      createdAtRelative: '2 days ago',
      createdAtCompact: '1/2/24',
      isRecent: true,
      age: 0,
      isAssignedToJob: true,
      jobId: 'job-123',
    },
  ];

  const mockMetadata = {
    currentPage: 1,
    pageSize: 10,
    totalCount: 20,
    totalPages: 2,
    hasPrevious: false,
    hasNext: true,
  };

  beforeEach(async () => {
    mockTaskService = jasmine.createSpyObj('TaskService', [
      'getTasks',
      'deleteTask',
    ]);
    mockPaginationFactory = jasmine.createSpyObj('PaginationFactory', [
      'create',
    ]);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    // Create mock pagination service
    const stateSubject = new BehaviorSubject({
      data: mockTasks,
      metadata: mockMetadata,
      loading: false,
      params: { pageNumber: 1, pageSize: DEFAULT_PAGE_SIZE },
    });

    mockPaginationService = jasmine.createSpyObj('PaginatedDataService', [
      'getData',
      'getMetadata',
      'isLoading',
      'getCurrentParams',
      'refresh',
      'updateParams',
      'setPage',
      'setPageSize',
      'setSort',
      'clearFilters',
      'destroy',
    ]);

    mockPaginationService.state$ = stateSubject.asObservable() as any;
    mockPaginationService.getData.and.returnValue(mockTasks);
    mockPaginationService.getMetadata.and.returnValue(mockMetadata);
    mockPaginationService.isLoading.and.returnValue(false);
    mockPaginationService.getCurrentParams.and.returnValue({
      pageNumber: 1,
      pageSize: DEFAULT_PAGE_SIZE,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    });

    mockPaginationFactory.create.and.returnValue(mockPaginationService);

    await TestBed.configureTestingModule({
      declarations: [TaskListComponent],
      providers: [
        { provide: TaskService, useValue: mockTaskService },
        { provide: PaginationFactory, useValue: mockPaginationFactory },
        { provide: Router, useValue: mockRouter },
      ],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    fixture = TestBed.createComponent(TaskListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should create pagination service on init', () => {
      fixture.detectChanges();

      expect(mockPaginationFactory.create).toHaveBeenCalledWith(
        jasmine.any(Function),
        {
          pageSize: DEFAULT_PAGE_SIZE,
          sortBy: 'createdAt',
          sortDirection: 'desc',
        }
      );
      expect(component.paginationService).toBeDefined();
    });

    it('should call refreshTasks on init', () => {
      spyOn(component, 'refreshTasks');
      fixture.detectChanges();

      expect(component.refreshTasks).toHaveBeenCalled();
    });

    it('should initialize with empty search and filter', () => {
      expect(component.searchText).toBe('');
      expect(component.statusFilter).toBe('');
    });
  });

  describe('Cleanup', () => {
    it('should destroy pagination service on component destroy', () => {
      fixture.detectChanges();
      component.ngOnDestroy();

      expect(mockPaginationService.destroy).toHaveBeenCalled();
    });
  });

  describe('Data Display', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should get filtered tasks from pagination service', () => {
      expect(component.filteredTasks).toEqual(mockTasks);
      expect(mockPaginationService.getData).toHaveBeenCalled();
    });

    it('should get loading state from pagination service', () => {
      expect(component.loading).toBe(false);
      expect(mockPaginationService.isLoading).toHaveBeenCalled();
    });

    it('should get pagination metadata', () => {
      expect(component.paginationMetadata).toEqual(mockMetadata);
      expect(mockPaginationService.getMetadata).toHaveBeenCalled();
    });

    it('should get current page', () => {
      expect(component.currentPage).toBe(1);
    });

    it('should get page size', () => {
      expect(component.pageSize).toBe(DEFAULT_PAGE_SIZE);
    });
  });

  describe('Filtering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should filter tasks with search text', () => {
      component.searchText = 'important';
      component.filterTasks();

      expect(mockPaginationService.updateParams).toHaveBeenCalledWith({
        searchTerm: 'important',
        status: undefined,
      });
    });

    it('should filter tasks by status', () => {
      component.statusFilter = '0'; // Pending
      component.filterTasks();

      expect(mockPaginationService.updateParams).toHaveBeenCalledWith({
        searchTerm: undefined,
        status: 0,
      });
    });

    it('should filter with both search and status', () => {
      component.searchText = 'test';
      component.statusFilter = '1'; // InProgress
      component.filterTasks();

      expect(mockPaginationService.updateParams).toHaveBeenCalledWith({
        searchTerm: 'test',
        status: 1,
      });
    });

    it('should clear all filters', () => {
      component.searchText = 'test';
      component.statusFilter = '1';

      component.clearFilters();

      expect(component.searchText).toBe('');
      expect(component.statusFilter).toBe('');
      expect(mockPaginationService.clearFilters).toHaveBeenCalledWith(true);
    });
  });

  describe('Pagination', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should change page', () => {
      component.onPageChange(2);

      expect(mockPaginationService.setPage).toHaveBeenCalledWith(2);
    });

    it('should change page size', () => {
      component.onPageSizeChange(20);

      expect(mockPaginationService.setPageSize).toHaveBeenCalledWith(20);
    });

    it('should update page size via setter', () => {
      component.pageSize = 25;

      expect(mockPaginationService.setPageSize).toHaveBeenCalledWith(25);
    });

    it('should calculate visible pages correctly', () => {
      const pages = component.visiblePages;

      expect(pages).toContain(1);
      expect(pages).toContain(2);
    });

    it('should handle pagination with ellipsis for many pages', () => {
      mockPaginationService.getMetadata.and.returnValue({
        ...mockMetadata,
        currentPage: 5,
        totalPages: 10,
        totalCount: 100,
      });

      const pages = component.visiblePages;

      expect(pages).toContain(-1); // Ellipsis marker
      expect(pages).toContain(1); // First page
      expect(pages).toContain(10); // Last page
    });
  });

  describe('Sorting', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should toggle sort direction when sorting by same column', () => {
      mockPaginationService.getCurrentParams.and.returnValue({
        pageNumber: 1,
        pageSize: 10,
        sortBy: 'title',
        sortDirection: 'asc',
      });

      component.onSortChange('title');

      expect(mockPaginationService.setSort).toHaveBeenCalledWith(
        'title',
        'desc'
      );
    });

    it('should set ascending direction when sorting by new column', () => {
      mockPaginationService.getCurrentParams.and.returnValue({
        pageNumber: 1,
        pageSize: 10,
        sortBy: 'createdAt',
        sortDirection: 'desc',
      });

      component.onSortChange('title');

      expect(mockPaginationService.setSort).toHaveBeenCalledWith(
        'title',
        'asc'
      );
    });
  });

  describe('Task Actions', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should navigate to create task page', () => {
      component.createTask();

      expect(mockRouter.navigate).toHaveBeenCalledWith([ROUTES.TASK_CREATE]);
    });

    it('should open edit modal with selected task', () => {
      const task = mockTasks[0];
      component.editTask(task);

      expect(component.selectedTask).toBe(task);
      expect(component.isEditMode).toBe(true);
      expect(component.showTaskForm).toBe(true);
    });

    it('should open view modal with selected task', () => {
      const task = mockTasks[0];
      component.viewTask(task);

      expect(component.selectedTask).toBe(task);
      expect(component.showTaskDetail).toBe(true);
    });

    it('should delete task after confirmation', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      mockTaskService.deleteTask.and.returnValue(of(true));

      const task = mockTasks[0];
      component.deleteTask(task);

      expect(window.confirm).toHaveBeenCalledWith(
        `Are you sure you want to delete the task "${task.title}"?`
      );
      expect(mockTaskService.deleteTask).toHaveBeenCalledWith(task.id);
    });

    it('should not delete task if user cancels', () => {
      spyOn(window, 'confirm').and.returnValue(false);

      const task = mockTasks[0];
      component.deleteTask(task);

      expect(mockTaskService.deleteTask).not.toHaveBeenCalled();
    });

    it('should refresh tasks after successful deletion', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      mockTaskService.deleteTask.and.returnValue(of(true));
      spyOn(component, 'refreshTasks');

      const task = mockTasks[0];
      component.deleteTask(task);

      expect(component.refreshTasks).toHaveBeenCalled();
    });

    it('should handle deletion error gracefully', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      spyOn(window, 'alert');
      mockTaskService.deleteTask.and.returnValue(
        throwError(() => new Error('Delete failed'))
      );

      const task = mockTasks[0];
      component.deleteTask(task);

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to delete task. Please try again.'
      );
    });
  });

  describe('Modal Handling', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should close task form and refresh on save', () => {
      spyOn(component, 'refreshTasks');
      component.showTaskForm = true;
      component.selectedTask = mockTasks[0];

      component.onTaskSaved(mockTasks[0]);

      expect(component.showTaskForm).toBe(false);
      expect(component.selectedTask).toBeNull();
      expect(component.refreshTasks).toHaveBeenCalled();
    });

    it('should close task form on cancel', () => {
      component.showTaskForm = true;
      component.selectedTask = mockTasks[0];

      component.onTaskFormCanceled();

      expect(component.showTaskForm).toBe(false);
      expect(component.selectedTask).toBeNull();
    });

    it('should close task detail modal', () => {
      component.showTaskDetail = true;
      component.selectedTask = mockTasks[0];

      component.onTaskDetailClosed();

      expect(component.showTaskDetail).toBe(false);
      expect(component.selectedTask).toBeNull();
    });
  });

  describe('Track By Function', () => {
    it('should track tasks by id', () => {
      const task = mockTasks[0];
      const result = component.trackByTaskId(0, task);

      expect(result).toBe(task.id);
    });
  });

  describe('Error Handling', () => {
    it('should handle pagination state errors', () => {
      spyOn(console, 'error');
      const errorSubject = new BehaviorSubject({ error: 'Test error' });

      mockPaginationService.state$ = errorSubject.asObservable() as any;

      fixture.detectChanges();

      // The subscription should handle the error without crashing
      expect(component.paginationService).toBeDefined();
    });
  });

  describe('Refresh', () => {
    it('should refresh tasks', () => {
      fixture.detectChanges();

      component.refreshTasks();

      expect(mockPaginationService.refresh).toHaveBeenCalled();
    });
  });
});
