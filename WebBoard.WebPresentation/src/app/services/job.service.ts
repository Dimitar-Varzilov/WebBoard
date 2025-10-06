import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { JobDto, JobDtoRaw, CreateJobRequestDto, AvailableTaskDto } from '../models';
import { JobModelFactory } from '../factories/model.factory';
import { JOBS_ENDPOINTS } from '../constants/endpoints';

@Injectable({
  providedIn: 'root',
})
export class JobService {
  constructor(private http: HttpClient) {}

  /**
   * Get all jobs with computed properties
   */
  getAllJobs(): Observable<JobDto[]> {
    return this.http.get<JobDtoRaw[]>(JOBS_ENDPOINTS.GET_ALL)
      .pipe(
        map(rawJobs => JobModelFactory.fromApiResponseArray(rawJobs))
      );
  }

  /**
   * Get job by ID with computed properties
   */
  getJobById(id: string): Observable<JobDto> {
    return this.http.get<JobDtoRaw>(JOBS_ENDPOINTS.GET_BY_ID(id))
      .pipe(
        map(rawJob => JobModelFactory.fromApiResponse(rawJob))
      );
  }

  /**
   * Create job
   */
  createJob(createJobRequest: CreateJobRequestDto): Observable<JobDto> {
    return this.http.post<JobDtoRaw>(JOBS_ENDPOINTS.CREATE, createJobRequest)
      .pipe(
        map(rawJob => JobModelFactory.fromApiResponse(rawJob))
      );
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
  getAvailableTasksForJob(jobType: string): Observable<AvailableTaskDto[]> {
    return this.http.get<AvailableTaskDto[]>(JOBS_ENDPOINTS.GET_AVAILABLE_TASKS, {
      params: { jobType }
    });
  }
}
