import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  TaskDto,
  TaskDtoRaw,
  CreateTaskRequestDto,
  UpdateTaskRequestDto,
  TaskItemStatus,
  PagedResult,
  TaskQueryParameters,
} from '../models';
import { TaskModelFactory } from '../factories/model.factory';
import { TASKS_ENDPOINTS } from '../constants/endpoints';
import { HttpParamsBuilder } from '../utils/http-params.utils';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  constructor(private http: HttpClient) {}

  /**
   * Get paginated tasks with filtering and sorting
   * Backend: GET /api/tasks?pageNumber={pageNumber}&pageSize={pageSize}&sortBy={sortBy}&sortDirection={sortDirection}&searchTerm={searchTerm}&status={status}&hasJob={hasJob}
   */
  getTasks(parameters: TaskQueryParameters): Observable<PagedResult<TaskDto>> {
    const params = HttpParamsBuilder.fromQueryParams(parameters);

    return this.http
      .get<PagedResult<TaskDtoRaw>>(TASKS_ENDPOINTS.BASE, { params })
      .pipe(
        map((result) => ({
          items: TaskModelFactory.fromApiResponseArray(result.items),
          metadata: result.metadata,
        }))
      );
  }

  /**
   * Get tasks by status with computed properties
   * Backend: GET /api/tasks/status/{status}
   */
  getTasksByStatus(status: TaskItemStatus): Observable<TaskDto[]> {
    return this.http
      .get<TaskDtoRaw[]>(TASKS_ENDPOINTS.GET_BY_STATUS(status))
      .pipe(map((rawTasks) => TaskModelFactory.fromApiResponseArray(rawTasks)));
  }

  /**
   * Get task by ID with computed properties
   * Backend: GET /api/tasks/{id}
   */
  getTaskById(id: string): Observable<TaskDto> {
    return this.http
      .get<TaskDtoRaw>(TASKS_ENDPOINTS.GET_BY_ID(id))
      .pipe(map((rawTask) => TaskModelFactory.fromApiResponse(rawTask)));
  }

  /**
   * Create task
   * Backend: POST /api/tasks
   */
  createTask(createTaskRequest: CreateTaskRequestDto): Observable<TaskDto> {
    return this.http
      .post<TaskDtoRaw>(TASKS_ENDPOINTS.CREATE, createTaskRequest)
      .pipe(map((rawTask) => TaskModelFactory.fromApiResponse(rawTask)));
  }

  /**
   * Update task
   * Backend: PUT /api/tasks/{id}
   */
  updateTask(
    id: string,
    updateTaskRequest: UpdateTaskRequestDto
  ): Observable<TaskDto> {
    return this.http
      .put<TaskDtoRaw>(TASKS_ENDPOINTS.UPDATE(id), updateTaskRequest)
      .pipe(map((rawTask) => TaskModelFactory.fromApiResponse(rawTask)));
  }

  /**
   * Delete task
   * Backend: DELETE /api/tasks/{id}
   */
  deleteTask(id: string): Observable<boolean> {
    return this.http.delete<boolean>(TASKS_ENDPOINTS.DELETE(id));
  }

  /**
   * Get task count by status (returns number, no model factory needed)
   * Backend: GET /api/tasks/status/{status}/count
   */
  getTaskCountByStatus(status: TaskItemStatus): Observable<number> {
    return this.http.get<number>(TASKS_ENDPOINTS.GET_COUNT_BY_STATUS(status));
  }
}
