import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TaskDto, CreateTaskRequestDto, UpdateTaskRequestDto } from '../models';
import { TASKS_ENDPOINTS } from '../constants/endpoints';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  constructor(private http: HttpClient) {}

  /**
   * Get all tasks
   * @returns Observable<TaskDto[]>
   */
  getAllTasks(): Observable<TaskDto[]> {
    return this.http.get<TaskDto[]>(TASKS_ENDPOINTS.GET_ALL);
  }

  /**
   * Get task by ID
   * @param id Task ID
   * @returns Observable<TaskDto>
   */
  getTaskById(id: string): Observable<TaskDto> {
    return this.http.get<TaskDto>(TASKS_ENDPOINTS.GET_BY_ID(id));
  }

  /**
   * Create a new task
   * @param createTaskRequest Task creation request
   * @returns Observable<TaskDto>
   */
  createTask(createTaskRequest: CreateTaskRequestDto): Observable<TaskDto> {
    return this.http.post<TaskDto>(TASKS_ENDPOINTS.CREATE, createTaskRequest);
  }

  /**
   * Update an existing task
   * @param id Task ID
   * @param updateTaskRequest Task update request
   * @returns Observable<TaskDto>
   */
  updateTask(
    id: string,
    updateTaskRequest: UpdateTaskRequestDto
  ): Observable<TaskDto> {
    return this.http.put<TaskDto>(
      TASKS_ENDPOINTS.UPDATE(id),
      updateTaskRequest
    );
  }

  /**
   * Delete a task
   * @param id Task ID
   * @returns Observable<void>
   */
  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(TASKS_ENDPOINTS.DELETE(id));
  }
}
