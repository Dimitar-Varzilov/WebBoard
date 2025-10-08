import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  JobDto,
  JobDtoRaw,
  CreateJobRequestDto,
  UpdateJobRequestDto,
  AvailableTaskDto,
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
   * Backend: GET /api/jobs?pageNumber={pageNumber}&pageSize={pageSize}&sortBy={sortBy}&sortDirection={sortDirection}&searchTerm={searchTerm}&status={status}&jobType={jobType}
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
   * Backend: GET /api/jobs/{id}
   */
  getJobById(id: string): Observable<JobDto> {
    return this.http
      .get<JobDtoRaw>(JOBS_ENDPOINTS.GET_BY_ID(id))
      .pipe(map((rawJob) => JobModelFactory.fromApiResponse(rawJob)));
  }

  /**
   * Create job
   * Backend: POST /api/jobs
   */
  createJob(createJobRequest: CreateJobRequestDto): Observable<JobDto> {
    return this.http
      .post<JobDtoRaw>(JOBS_ENDPOINTS.CREATE, createJobRequest)
      .pipe(map((rawJob) => JobModelFactory.fromApiResponse(rawJob)));
  }

  /**
   * Update job (only queued jobs can be updated)
   * Backend: PUT /api/jobs/{id}
   */
  updateJob(id: string, updateJobRequest: UpdateJobRequestDto): Observable<JobDto> {
    return this.http
      .put<JobDtoRaw>(JOBS_ENDPOINTS.UPDATE(id), updateJobRequest)
      .pipe(map((rawJob) => JobModelFactory.fromApiResponse(rawJob)));
  }

  /**
   * Delete job (only queued jobs can be deleted)
   * Backend: DELETE /api/jobs/{id}
   */
  deleteJob(id: string): Observable<void> {
    return this.http.delete<void>(JOBS_ENDPOINTS.DELETE(id));
  }

  /**
   * Get pending tasks count
   * Backend: GET /api/jobs/validation/pending-tasks-count
   */
  getPendingTasksCount(): Observable<number> {
    return this.http.get<number>(JOBS_ENDPOINTS.GET_PENDING_TASKS_COUNT);
  }

  /**
   * Get available tasks for job creation based on job type
   * Backend: GET /api/jobs/validation/available-tasks?jobType={jobType}
   */
  getAvailableTasksForJob(jobType: string): Observable<AvailableTaskDto[]> {
    const params = HttpParamsBuilder.build({ jobType });
    return this.http.get<AvailableTaskDto[]>(
      JOBS_ENDPOINTS.GET_AVAILABLE_TASKS,
      { params }
    );
  }
}
