import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { JobDto, CreateJobRequestDto } from '../models';
import { JOBS_ENDPOINTS } from '../constants/endpoints';

@Injectable({
  providedIn: 'root',
})
export class JobService {
  constructor(private http: HttpClient) {}

  /**
   * Get all jobs
   * @returns Observable<JobDto[]>
   */
  getAllJobs(): Observable<JobDto[]> {
    return this.http.get<JobDto[]>(JOBS_ENDPOINTS.GET_ALL);
  }

  /**
   * Get job by ID
   * @param id Job ID
   * @returns Observable<JobDto>
   */
  getJobById(id: string): Observable<JobDto> {
    return this.http.get<JobDto>(JOBS_ENDPOINTS.GET_BY_ID(id));
  }

  /**
   * Create a new job
   * @param createJobRequest Job creation request
   * @returns Observable<JobDto>
   */
  createJob(createJobRequest: CreateJobRequestDto): Observable<JobDto> {
    return this.http.post<JobDto>(JOBS_ENDPOINTS.CREATE, createJobRequest);
  }
}
