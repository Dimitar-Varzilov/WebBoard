import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  JobDto,
  JobDtoRaw,
  CreateJobRequestDto,
  TaskDtoRaw,
  PagedResult,
  JobQueryParameters,
} from '../models';
import { JobModelFactory } from '../factories/model.factory';
import { JOBS_ENDPOINTS } from '../constants/endpoints';
import { HttpParamsBuilder } from '../utils/http-params.utils';

@Injectable({
  providedIn: 'root',
})
export class JobService {
  constructor(private http: HttpClient) {}

  /**
   * Get paginated jobs with filtering and sorting
   */
  getJobs(parameters: JobQueryParameters): Observable<PagedResult<JobDto>> {
    const params = HttpParamsBuilder.fromQueryParams(parameters);

    return this.http
      .get<PagedResult<JobDtoRaw>>(JOBS_ENDPOINTS.BASE, { params })
      .pipe(
        map((result) => ({
          items: JobModelFactory.fromApiResponseArray(result.items),
          metadata: result.metadata,
        }))
      );
  }

  /**
   * Get job by ID with computed properties
   */
  getJobById(id: string): Observable<JobDto> {
    return this.http
      .get<JobDtoRaw>(JOBS_ENDPOINTS.GET_BY_ID(id))
      .pipe(map((rawJob) => JobModelFactory.fromApiResponse(rawJob)));
  }

  /**
   * Create job
   */
  createJob(createJobRequest: CreateJobRequestDto): Observable<JobDto> {
    return this.http
      .post<JobDtoRaw>(JOBS_ENDPOINTS.CREATE, createJobRequest)
      .pipe(map((rawJob) => JobModelFactory.fromApiResponse(rawJob)));
  }

  /**
   * Get pending tasks count
   */
  getPendingTasksCount(): Observable<number> {
    return this.http.get<number>(JOBS_ENDPOINTS.GET_PENDING_TASKS_COUNT);
  }

  /**
   * Get available tasks for job creation based on job type
   */
  getAvailableTasksForJob(jobType: string): Observable<TaskDtoRaw[]> {
    const params = HttpParamsBuilder.build({ jobType });
    return this.http.get<TaskDtoRaw[]>(JOBS_ENDPOINTS.GET_AVAILABLE_TASKS, {
      params,
    });
  }
}
