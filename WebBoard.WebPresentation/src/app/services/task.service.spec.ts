import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { TaskService } from './task.service';
import {
  TaskDto,
  CreateTaskRequestDto,
  UpdateTaskRequestDto,
  TaskItemStatus,
} from '../models';
import { TASKS_ENDPOINTS } from '../constants/endpoints';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  const mockTask: TaskDto = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    title: 'Test Task',
    description: 'Test Description',
    status: TaskItemStatus.Pending,
    createdAt: '2024-01-01T00:00:00Z',
    createdAtDate: new Date('2024-01-01T00:00:00Z'),
    createdAtDisplay: 'Jan 1, 2024',
    createdAtRelative: '1 day ago',
    createdAtCompact: '1/1/24',
    isRecent: false,
    age: 1,
    isAssignedToJob: false,
  };

  const mockTasks: TaskDto[] = [
    mockTask,
    {
      id: '123e4567-e89b-12d3-a456-426614174001',
      title: 'Another Task',
      description: 'Another Description',
      status: TaskItemStatus.InProgress,
      createdAt: '2024-01-02T00:00:00Z',
      createdAtDate: new Date('2024-01-02T00:00:00Z'),
      createdAtDisplay: 'Jan 2, 2024',
      createdAtRelative: '2 days ago',
      createdAtCompact: '1/2/24',
      isRecent: false,
      age: 2,
      isAssignedToJob: false,
    },
  ];

  const createTaskRequest: CreateTaskRequestDto = {
    title: 'New Task',
    description: 'New Task Description',
  };

  const updateTaskRequest: UpdateTaskRequestDto = {
    title: 'Updated Task',
    description: 'Updated Description',
    status: TaskItemStatus.Completed,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TaskService],
    });
    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getTasks', () => {
    it('should return paginated tasks', () => {
      const mockPagedResult = {
        items: mockTasks,
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1,
        },
      };

      service.getTasks({ pageNumber: 1, pageSize: 10 }).subscribe((result) => {
        expect(result.items.length).toBe(2);
        expect(result.items[0].title).toBe('Test Task');
        expect(result.items[1].title).toBe('Another Task');
        expect(result.items[0].createdAtDate).toBeDefined();
        expect(result.items[0].createdAtDisplay).toBeDefined();
        expect(result.metadata.totalCount).toBe(2);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });

    it('should return empty array when no tasks exist', () => {
      const emptyPagedResult = {
        items: [],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
        },
      };

      service.getTasks({ pageNumber: 1, pageSize: 10 }).subscribe((result) => {
        expect(result.items).toEqual([]);
        expect(result.items.length).toBe(0);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      expect(req.request.method).toBe('GET');
      req.flush(emptyPagedResult);
    });

    it('should handle HTTP error when getting tasks', () => {
      service.getTasks({ pageNumber: 1, pageSize: 10 }).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Internal Server Error');
        },
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      req.flush('Server error', {
        status: 500,
        statusText: 'Internal Server Error',
      });
    });
  });

  describe('getTaskById', () => {
    it('should return a task when valid id is provided', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';

      service.getTaskById(taskId).subscribe((task) => {
        expect(task.id).toBe(taskId);
        expect(task.title).toBe('Test Task');
        expect(task.status).toBe(TaskItemStatus.Pending);
        expect(task.createdAtDate).toBeDefined();
        expect(task.createdAtDisplay).toBeDefined();
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.GET_BY_ID(taskId));
      expect(req.request.method).toBe('GET');
      expect(req.request.url).toBe(TASKS_ENDPOINTS.GET_BY_ID(taskId));
      req.flush(mockTask);
    });

    it('should handle HTTP error response', () => {
      const taskId = 'invalid-id';

      service.getTaskById(taskId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.GET_BY_ID(taskId));
      expect(req.request.method).toBe('GET');
      req.flush('Task not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('createTask', () => {
    it('should create a new task and return it', () => {
      service.createTask(createTaskRequest).subscribe((task) => {
        expect(task.title).toBe('Test Task');
        expect(task.status).toBe(TaskItemStatus.Pending);
        expect(task.createdAtDate).toBeDefined();
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.CREATE);
      expect(req.request.method).toBe('POST');
      expect(req.request.url).toBe(TASKS_ENDPOINTS.CREATE);
      expect(req.request.body).toEqual(createTaskRequest);
      req.flush(mockTask);
    });

    it('should handle validation error when creating task', () => {
      const invalidRequest: CreateTaskRequestDto = {
        title: 'A', // Too short
        description: '',
      };

      service.createTask(invalidRequest).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(400);
          expect(error.statusText).toBe('Bad Request');
        },
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.CREATE);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(invalidRequest);
      req.flush('Validation failed', {
        status: 400,
        statusText: 'Bad Request',
      });
    });
  });

  describe('updateTask', () => {
    it('should update an existing task and return it', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';
      const updatedTask = { ...mockTask, ...updateTaskRequest };

      service.updateTask(taskId, updateTaskRequest).subscribe((task) => {
        expect(task.title).toBe(updateTaskRequest.title);
        expect(task.description).toBe(updateTaskRequest.description);
        expect(task.status).toBe(updateTaskRequest.status);
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.UPDATE(taskId));
      expect(req.request.method).toBe('PUT');
      expect(req.request.url).toBe(TASKS_ENDPOINTS.UPDATE(taskId));
      expect(req.request.body).toEqual(updateTaskRequest);
      req.flush(updatedTask);
    });

    it('should handle task not found error when updating', () => {
      const taskId = 'non-existent-id';

      service.updateTask(taskId, updateTaskRequest).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.UPDATE(taskId));
      expect(req.request.method).toBe('PUT');
      req.flush('Task not found', { status: 404, statusText: 'Not Found' });
    });

    it('should handle validation error when updating task', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';
      const invalidUpdate: UpdateTaskRequestDto = {
        title: '', // Invalid empty title
        description: 'Valid description',
        status: TaskItemStatus.Completed,
      };

      service.updateTask(taskId, invalidUpdate).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(400);
          expect(error.statusText).toBe('Bad Request');
        },
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.UPDATE(taskId));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(invalidUpdate);
      req.flush('Validation failed', {
        status: 400,
        statusText: 'Bad Request',
      });
    });
  });

  describe('deleteTask', () => {
    it('should delete a task successfully', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';

      service.deleteTask(taskId).subscribe((response) => {
        expect(response).toBe(true);
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.DELETE(taskId));
      expect(req.request.method).toBe('DELETE');
      expect(req.request.url).toBe(TASKS_ENDPOINTS.DELETE(taskId));
      expect(req.request.body).toBeNull();
      req.flush(true);
    });

    it('should handle task not found error when deleting', () => {
      const taskId = 'non-existent-id';

      service.deleteTask(taskId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.DELETE(taskId));
      expect(req.request.method).toBe('DELETE');
      req.flush('Task not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('endpoint construction', () => {
    it('should construct correct endpoint URLs', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';

      expect(TASKS_ENDPOINTS.BASE).toContain('/tasks');
      expect(TASKS_ENDPOINTS.GET_BY_ID(taskId)).toContain(`/tasks/${taskId}`);
      expect(TASKS_ENDPOINTS.CREATE).toContain('/tasks');
      expect(TASKS_ENDPOINTS.UPDATE(taskId)).toContain(`/tasks/${taskId}`);
      expect(TASKS_ENDPOINTS.DELETE(taskId)).toContain(`/tasks/${taskId}`);
    });
  });

  describe('getTasks with complex filtering', () => {
    it('should send searchTerm parameter correctly', () => {
      const mockPagedResult = {
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        },
      };

      service
        .getTasks({
          pageNumber: 1,
          pageSize: 10,
          searchTerm: 'test',
        })
        .subscribe((result) => {
          expect(result.items.length).toBe(1);
        });

      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('searchTerm') &&
          request.params.get('searchTerm') === 'test'
        );
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });

    it('should send status filter parameter correctly', () => {
      const mockPagedResult = {
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        },
      };

      service
        .getTasks({
          pageNumber: 1,
          pageSize: 10,
          status: TaskItemStatus.Pending,
        })
        .subscribe((result) => {
          expect(result.items.length).toBe(1);
        });

      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('status') &&
          request.params.get('status') === TaskItemStatus.Pending.toString()
        );
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });

    it('should send hasJob filter parameter correctly', () => {
      const mockPagedResult = {
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        },
      };

      service
        .getTasks({
          pageNumber: 1,
          pageSize: 10,
          hasJob: true,
        })
        .subscribe((result) => {
          expect(result.items.length).toBe(1);
        });

      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('hasJob') &&
          request.params.get('hasJob') === 'true'
        );
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });

    it('should send multiple filter parameters together', () => {
      const mockPagedResult = {
        items: [mockTask],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        },
      };

      service
        .getTasks({
          pageNumber: 1,
          pageSize: 10,
          searchTerm: 'test',
          status: TaskItemStatus.Pending,
          hasJob: false,
        })
        .subscribe((result) => {
          expect(result.items.length).toBe(1);
        });

      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('searchTerm') &&
          request.params.get('searchTerm') === 'test' &&
          request.params.has('status') &&
          request.params.get('status') === TaskItemStatus.Pending.toString() &&
          request.params.has('hasJob') &&
          request.params.get('hasJob') === 'false'
        );
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });

    it('should send sorting parameters correctly', () => {
      const mockPagedResult = {
        items: mockTasks,
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1,
        },
      };

      service
        .getTasks({
          pageNumber: 1,
          pageSize: 10,
          sortBy: 'createdAt',
          sortDirection: 'desc',
        })
        .subscribe((result) => {
          expect(result.items.length).toBe(2);
        });

      const req = httpMock.expectOne((request) => {
        return (
          request.url.includes(TASKS_ENDPOINTS.BASE) &&
          request.params.has('sortBy') &&
          request.params.get('sortBy') === 'createdAt' &&
          request.params.has('sortDirection') &&
          request.params.get('sortDirection') === 'desc'
        );
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });
  });

  describe('getTasksByStatus', () => {
    it('should return tasks by status', () => {
      const pendingTasks = [mockTask];

      service.getTasksByStatus(TaskItemStatus.Pending).subscribe((tasks) => {
        expect(tasks.length).toBe(1);
        expect(tasks[0].status).toBe(TaskItemStatus.Pending);
        expect(tasks[0].createdAtDate).toBeDefined();
      });

      const req = httpMock.expectOne(
        TASKS_ENDPOINTS.GET_BY_STATUS(TaskItemStatus.Pending)
      );
      expect(req.request.method).toBe('GET');
      req.flush(pendingTasks);
    });

    it('should return empty array when no tasks match status', () => {
      service.getTasksByStatus(TaskItemStatus.Completed).subscribe((tasks) => {
        expect(tasks).toEqual([]);
      });

      const req = httpMock.expectOne(
        TASKS_ENDPOINTS.GET_BY_STATUS(TaskItemStatus.Completed)
      );
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });

  describe('getTaskCountByStatus', () => {
    it('should return task count by status', () => {
      service
        .getTaskCountByStatus(TaskItemStatus.Pending)
        .subscribe((count) => {
          expect(count).toBe(5);
        });

      const req = httpMock.expectOne(
        TASKS_ENDPOINTS.GET_COUNT_BY_STATUS(TaskItemStatus.Pending)
      );
      expect(req.request.method).toBe('GET');
      req.flush(5);
    });

    it('should return 0 when no tasks match status', () => {
      service
        .getTaskCountByStatus(TaskItemStatus.Completed)
        .subscribe((count) => {
          expect(count).toBe(0);
        });

      const req = httpMock.expectOne(
        TASKS_ENDPOINTS.GET_COUNT_BY_STATUS(TaskItemStatus.Completed)
      );
      expect(req.request.method).toBe('GET');
      req.flush(0);
    });
  });

  describe('Model factory integration', () => {
    it('should transform raw task data with computed properties', () => {
      const rawTask = {
        id: 'task-123',
        title: 'Test Task',
        description: 'Test Description',
        status: TaskItemStatus.Pending,
        createdAt: '2024-01-01T00:00:00Z',
      };

      service.getTaskById('task-123').subscribe((task) => {
        expect(task.createdAtDate).toBeInstanceOf(Date);
        expect(task.createdAtDisplay).toBeDefined();
        expect(task.createdAtRelative).toBeDefined();
        expect(task.createdAtCompact).toBeDefined();
        expect(task.isRecent).toBeDefined();
        expect(task.age).toBeDefined();
        expect(task.isAssignedToJob).toBeDefined();
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.GET_BY_ID('task-123'));
      req.flush(rawTask);
    });

    it('should transform array of raw task data with computed properties', () => {
      const rawTasks = [
        {
          id: 'task-1',
          title: 'Task 1',
          description: 'Description 1',
          status: TaskItemStatus.Pending,
          createdAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'task-2',
          title: 'Task 2',
          description: 'Description 2',
          status: TaskItemStatus.InProgress,
          createdAt: '2024-01-02T00:00:00Z',
          jobId: 'job-123',
        },
      ];

      service.getTasks({ pageNumber: 1, pageSize: 10 }).subscribe((result) => {
        expect(result.items[0].createdAtDate).toBeInstanceOf(Date);
        expect(result.items[0].isAssignedToJob).toBe(false);
        expect(result.items[1].isAssignedToJob).toBe(true);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(TASKS_ENDPOINTS.BASE)
      );
      req.flush({
        items: rawTasks,
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1,
        },
      });
    });
  });
});
