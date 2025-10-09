import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { Location } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TaskCreateComponent } from '../components/tasks/task-create/task-create.component';
import { TaskFormComponent } from '../components/tasks/task-form/task-form.component';
import { TaskListComponent } from '../components/tasks/task-list/task-list.component';
import { ROUTES, TASKS_ENDPOINTS } from '../constants';
import { TaskDto, TaskItemStatus } from '../models';
import { TaskService } from '../services';
import { PaginationFactory } from '../services/pagination-factory.service';

/**
 * End-to-End Integration Tests for Task CRUD Operations
 * Tests complete user workflows from UI interaction through service layer to API
 */
describe('Task E2E Integration Tests', () => {
  let httpMock: HttpTestingController;
  let taskService: TaskService;
  let router: Router;
  let location: Location;

  const mockTask: TaskDto = {
    id: 'task-123',
    title: 'Test Task',
    description: 'Test Description',
    status: TaskItemStatus.Pending,
    createdAt: '2024-01-15T12:00:00Z',
    createdAtDate: new Date('2024-01-15T12:00:00Z'),
    createdAtDisplay: 'Jan 15, 2024',
    createdAtRelative: '1 day ago',
    createdAtCompact: '1/15/24',
    isRecent: true,
    age: 0,
    isAssignedToJob: false,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TaskListComponent, TaskCreateComponent, TaskFormComponent],
      imports: [HttpClientTestingModule, ReactiveFormsModule, FormsModule],
      providers: [TaskService, PaginationFactory],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    taskService = TestBed.inject(TaskService);
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);

    spyOn(router, 'navigate');
    spyOn(location, 'back');
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Task Creation Workflow', () => {
    let fixture: ComponentFixture<TaskCreateComponent>;
    let component: TaskCreateComponent;

    beforeEach(() => {
      fixture = TestBed.createComponent(TaskCreateComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should complete full task creation flow: form validation → API call → navigation', () => {
      // Step 1: User fills form with valid data
      component.taskForm.setValue({
        title: 'New Task',
        description: 'New Description',
      });

      expect(component.taskForm.valid).toBe(true);

      // Step 2: User submits form
      component.onSubmit();

      // Step 3: API receives POST request
      const req = httpMock.expectOne(TASKS_ENDPOINTS.CREATE);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        title: 'New Task',
        description: 'New Description',
      });

      // Step 4: API responds with created task
      req.flush(mockTask);

      // Step 5: User navigates back in history
      expect(location.back).toHaveBeenCalled();
    });

    it('should prevent submission with invalid form data', () => {
      // User submits empty form
      component.onSubmit();

      // Form validation prevents API call
      httpMock.expectNone(TASKS_ENDPOINTS.CREATE);

      // Fields are marked as touched to show errors
      expect(component.taskForm.get('title')?.touched).toBe(true);
      expect(component.taskForm.get('description')?.touched).toBe(true);
    });

    it('should handle API validation errors during creation', () => {
      spyOn(window, 'alert');

      component.taskForm.setValue({
        title: 'Valid Title',
        description: 'Valid Description',
      });

      component.onSubmit();

      const req = httpMock.expectOne(TASKS_ENDPOINTS.CREATE);
      req.flush(
        { message: 'Title already exists' },
        { status: 400, statusText: 'Bad Request' }
      );

      expect(window.alert).toHaveBeenCalled();
      expect(component.submitting).toBe(false);
    });
  });

  describe('Task List, Filter, and Pagination Workflow', () => {
    let fixture: ComponentFixture<TaskListComponent>;
    let component: TaskListComponent;

    beforeEach(() => {
      fixture = TestBed.createComponent(TaskListComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should load initial tasks from API', () => {
      const mockPagedResult = {
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasPrevious: false,
          hasNext: false,
        },
      };

      // Initial load should request tasks
      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);

      // Tasks should be loaded into component
      expect(component.filteredTasks.length).toBe(1);
      expect(component.filteredTasks[0].title).toBe('Test Task');
    });

    it('should load tasks with pagination parameters', () => {
      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('pageNumber') &&
          request.params.has('pageSize')
        );
      });

      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');

      req.flush({
        items: [],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
          hasPrevious: false,
          hasNext: false,
        },
      });
    });
  });

  describe('Task Edit Workflow', () => {
    let fixture: ComponentFixture<TaskFormComponent>;
    let component: TaskFormComponent;

    beforeEach(() => {
      fixture = TestBed.createComponent(TaskFormComponent);
      component = fixture.componentInstance;

      // Configure for edit mode
      component.isEdit = true;
      component.task = mockTask;
      fixture.detectChanges();
    });

    it('should open edit modal → modify task → save changes', () => {
      // Verify form initialized with task data
      expect(component.taskForm.get('title')?.value).toBe(mockTask.title);
      expect(component.taskForm.get('status')?.value).toBe(mockTask.status);

      // User edits the task
      component.taskForm.patchValue({
        title: 'Updated Title',
        status: TaskItemStatus.Completed,
      });

      spyOn(component.save, 'emit');

      // User saves changes
      component.onSubmit();

      const req = httpMock.expectOne(TASKS_ENDPOINTS.UPDATE(mockTask.id));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body.title).toBe('Updated Title');
      expect(req.request.body.status).toBe(TaskItemStatus.Completed);

      const updatedTask = {
        ...mockTask,
        title: 'Updated Title',
        status: TaskItemStatus.Completed,
      };
      req.flush(updatedTask);

      // Verify save event was emitted
      expect(component.save.emit).toHaveBeenCalled();
    });
  });

  describe('Task Delete Workflow', () => {
    let fixture: ComponentFixture<TaskListComponent>;
    let component: TaskListComponent;

    const mockPagedResult = {
      items: [mockTask],
      metadata: {
        currentPage: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPrevious: false,
        hasNext: false,
      },
    };

    beforeEach(() => {
      fixture = TestBed.createComponent(TaskListComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      req.flush(mockPagedResult);
    });

    it('should confirm delete → call API → refresh list', () => {
      // User initiates delete with confirmation
      spyOn(window, 'confirm').and.returnValue(true);
      component.deleteTask(mockTask);

      // API receives delete request
      const deleteReq = httpMock.expectOne(TASKS_ENDPOINTS.DELETE(mockTask.id));
      expect(deleteReq.request.method).toBe('DELETE');
      deleteReq.flush(true);

      // List refreshes after delete
      const refreshReq = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      refreshReq.flush({
        items: [],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
          hasPrevious: false,
          hasNext: false,
        },
      });
    });

    it('should cancel delete when user rejects confirmation', () => {
      // User cancels delete
      spyOn(window, 'confirm').and.returnValue(false);
      component.deleteTask(mockTask);

      // No API call made
      httpMock.expectNone(TASKS_ENDPOINTS.DELETE(mockTask.id));

      // Task list unchanged
      expect(component.filteredTasks.length).toBe(1);
    });
  });

  describe('Sorting Workflow', () => {
    it('should initialize with default sort parameters', () => {
      const fixture = TestBed.createComponent(TaskListComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('sortBy') &&
          request.params.get('sortBy') === 'createdAt' &&
          request.params.get('sortDirection') === 'desc'
        );
      });

      req.flush({
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasPrevious: false,
          hasNext: false,
        },
      });

      expect(component.filteredTasks.length).toBe(1);
    });
  });

  describe('Complete Task Lifecycle', () => {
    it('should create task and navigate to list', () => {
      // Step 1: Create task
      const createFixture = TestBed.createComponent(TaskCreateComponent);
      const createComponent = createFixture.componentInstance;
      createFixture.detectChanges();

      createComponent.taskForm.setValue({
        title: 'Lifecycle Task',
        description: 'Test complete lifecycle',
      });
      createComponent.onSubmit();
      createFixture.detectChanges();

      const createReq = httpMock.expectOne(TASKS_ENDPOINTS.CREATE);
      const createdTask = { ...mockTask, title: 'Lifecycle Task' };
      createReq.flush(createdTask);

      expect(location.back).toHaveBeenCalled();
    });

    it('should list and delete task', () => {
      // Step 1: List tasks
      const listFixture = TestBed.createComponent(TaskListComponent);
      const listComponent = listFixture.componentInstance;
      listFixture.detectChanges();

      const listReq = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      const taskToDelete = { ...mockTask, title: 'Task to Delete' };
      listReq.flush({
        items: [taskToDelete],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasPrevious: false,
          hasNext: false,
        },
      });

      expect(listComponent.filteredTasks.length).toBe(1);

      // Step 2: Delete task
      spyOn(window, 'confirm').and.returnValue(true);
      listComponent.deleteTask(taskToDelete);
      listFixture.detectChanges();

      const deleteReq = httpMock.expectOne(
        TASKS_ENDPOINTS.DELETE(taskToDelete.id)
      );
      expect(deleteReq.request.method).toBe('DELETE');
      deleteReq.flush({});

      // List refreshes
      const refreshReq = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      refreshReq.flush({
        items: [],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
          hasPrevious: false,
          hasNext: false,
        },
      });
    });
  });

  describe('Task Assignment Workflow', () => {
    let fixture: ComponentFixture<TaskListComponent>;
    let component: TaskListComponent;

    beforeEach(() => {
      fixture = TestBed.createComponent(TaskListComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should load tasks and verify job assignment data', () => {
      const assignedTask = {
        ...mockTask,
        jobId: 'job-123',
        isAssignedToJob: true,
      };
      const mockPagedResult = {
        items: [assignedTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasPrevious: false,
          hasNext: false,
        },
      };

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      req.flush(mockPagedResult);

      // Verify task has job assignment
      expect(component.filteredTasks.length).toBe(1);
      expect(component.filteredTasks[0].jobId).toBe('job-123');
      expect(component.filteredTasks[0].isAssignedToJob).toBe(true);
    });
  });

  describe('Error Handling Workflows', () => {
    it('should handle network errors during task loading', () => {
      const fixture = TestBed.createComponent(TaskListComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );

      req.flush('Network error', {
        status: 500,
        statusText: 'Internal Server Error',
      });

      // Component should handle error gracefully
      expect(component.filteredTasks).toEqual([]);
    });

    it('should handle validation errors during task update', () => {
      const fixture = TestBed.createComponent(TaskFormComponent);
      const component = fixture.componentInstance;

      component.isEdit = true;
      component.task = mockTask;
      fixture.detectChanges();

      spyOn(window, 'alert');

      component.taskForm.patchValue({
        title: 'Updated Title',
      });

      component.onSubmit();

      const req = httpMock.expectOne(TASKS_ENDPOINTS.UPDATE(mockTask.id));
      req.flush(
        { message: 'Validation failed' },
        { status: 400, statusText: 'Bad Request' }
      );

      expect(window.alert).toHaveBeenCalled();
      expect(component.saving).toBe(false);
    });

    it('should handle delete errors', () => {
      const fixture = TestBed.createComponent(TaskListComponent);
      const component = fixture.componentInstance;

      const mockPagedResult = {
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasPrevious: false,
          hasNext: false,
        },
      };

      fixture.detectChanges();

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      req.flush(mockPagedResult);

      spyOn(window, 'confirm').and.returnValue(true);
      spyOn(window, 'alert');

      component.deleteTask(mockTask);

      const deleteReq = httpMock.expectOne(TASKS_ENDPOINTS.DELETE(mockTask.id));
      deleteReq.flush('Delete failed', {
        status: 500,
        statusText: 'Internal Server Error',
      });

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to delete task. Please try again.'
      );
    });
  });
});
