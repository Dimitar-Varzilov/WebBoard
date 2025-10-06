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
   */
  getTasksByStatus(status: TaskItemStatus): Observable<TaskDto[]> {
    return this.http
      .get<TaskDtoRaw[]>(TASKS_ENDPOINTS.GET_BY_STATUS(status))
      .pipe(map((rawTasks) => TaskModelFactory.fromApiResponseArray(rawTasks)));
  }

  /**
   * Get task by ID with computed properties
   */
  getTaskById(id: string): Observable<TaskDto> {
    return this.http
      .get<TaskDtoRaw>(TASKS_ENDPOINTS.GET_BY_ID(id))
      .pipe(map((rawTask) => TaskModelFactory.fromApiResponse(rawTask)));
  }

  /**
   * Create task
   */
  createTask(createTaskRequest: CreateTaskRequestDto): Observable<TaskDto> {
    return this.http
      .post<TaskDtoRaw>(TASKS_ENDPOINTS.CREATE, createTaskRequest)
      .pipe(map((rawTask) => TaskModelFactory.fromApiResponse(rawTask)));
  }

  /**
   * Update task
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
   */
  deleteTask(id: string): Observable<boolean> {
    return this.http.delete<boolean>(TASKS_ENDPOINTS.DELETE(id));
  }

  /**
   * Get task count by status (returns number, no model factory needed)
   */
  getTaskCountByStatus(status: TaskItemStatus): Observable<number> {
    return this.http.get<number>(TASKS_ENDPOINTS.GET_COUNT_BY_STATUS(status));
  }
}
