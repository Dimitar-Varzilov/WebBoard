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
    status: TaskItemStatus.OnHold,
    createdAt: new Date('2024-01-01T00:00:00Z'),
  };

  const mockTasks: TaskDto[] = [
    mockTask,
    {
      id: '123e4567-e89b-12d3-a456-426614174001',
      title: 'Another Task',
      description: 'Another Description',
      status: TaskItemStatus.InProgress,
      createdAt: new Date('2024-01-02T00:00:00Z'),
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

  describe('getAllTasks', () => {
    it('should return an array of tasks', () => {
      service.getAllTasks().subscribe((tasks) => {
        expect(tasks).toEqual(mockTasks);
        expect(tasks.length).toBe(2);
        expect(tasks[0].title).toBe('Test Task');
        expect(tasks[1].title).toBe('Another Task');
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.GET_ALL);
      expect(req.request.method).toBe('GET');
      expect(req.request.url).toBe(TASKS_ENDPOINTS.GET_ALL);
      req.flush(mockTasks);
    });

    it('should return empty array when no tasks exist', () => {
      service.getAllTasks().subscribe((tasks) => {
        expect(tasks).toEqual([]);
        expect(tasks.length).toBe(0);
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.GET_ALL);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });

    it('should handle HTTP error when getting all tasks', () => {
      service.getAllTasks().subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Internal Server Error');
        },
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.GET_ALL);
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
        expect(task).toEqual(mockTask);
        expect(task.id).toBe(taskId);
        expect(task.title).toBe('Test Task');
        expect(task.status).toBe(TaskItemStatus.OnHold);
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
        expect(task).toEqual(mockTask);
        expect(task.title).toBe('Test Task');
        expect(task.status).toBe(TaskItemStatus.OnHold);
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
        expect(response).toBeUndefined();
      });

      const req = httpMock.expectOne(TASKS_ENDPOINTS.DELETE(taskId));
      expect(req.request.method).toBe('DELETE');
      expect(req.request.url).toBe(TASKS_ENDPOINTS.DELETE(taskId));
      expect(req.request.body).toBeNull();
      req.flush(null, { status: 204, statusText: 'No Content' });
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

      expect(TASKS_ENDPOINTS.GET_ALL).toContain('/tasks');
      expect(TASKS_ENDPOINTS.GET_BY_ID(taskId)).toContain(`/tasks/${taskId}`);
      expect(TASKS_ENDPOINTS.CREATE).toContain('/tasks');
      expect(TASKS_ENDPOINTS.UPDATE(taskId)).toContain(`/tasks/${taskId}`);
      expect(TASKS_ENDPOINTS.DELETE(taskId)).toContain(`/tasks/${taskId}`);
    });
  });

  describe('HTTP headers', () => {
    it('should send correct headers for POST requests', () => {
      service.createTask(createTaskRequest).subscribe();

      const req = httpMock.expectOne(TASKS_ENDPOINTS.CREATE);
      expect(req.request.headers.get('Content-Type')).toBe('application/json');
      req.flush(mockTask);
    });

    it('should send correct headers for PUT requests', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';
      service.updateTask(taskId, updateTaskRequest).subscribe();

      const req = httpMock.expectOne(TASKS_ENDPOINTS.UPDATE(taskId));
      expect(req.request.headers.get('Content-Type')).toBe('application/json');
      req.flush(mockTask);
    });
  });
});
