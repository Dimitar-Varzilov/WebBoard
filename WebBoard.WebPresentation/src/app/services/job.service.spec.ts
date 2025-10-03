import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { JobService } from './job.service';
import { JobDto, CreateJobRequestDto, JobStatus } from '../models';
import { JOBS_ENDPOINTS } from '../constants/endpoints';

describe('JobService', () => {
  let service: JobService;
  let httpMock: HttpTestingController;

  const mockJob: JobDto = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    jobType: 'DataProcessing',
    status: JobStatus.Pending,
    createdAt: new Date('2024-01-01T00:00:00Z'),
  };

  const createJobRequest: CreateJobRequestDto = {
    jobType: 'DataProcessing',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [JobService],
    });
    service = TestBed.inject(JobService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getJobById', () => {
    it('should return a job when valid id is provided', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';

      service.getJobById(jobId).subscribe((job) => {
        expect(job).toEqual(mockJob);
        expect(job.id).toBe(jobId);
        expect(job.jobType).toBe('DataProcessing');
        expect(job.status).toBe(JobStatus.Pending);
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.GET_BY_ID(jobId));
      expect(req.request.method).toBe('GET');
      expect(req.request.url).toBe(JOBS_ENDPOINTS.GET_BY_ID(jobId));
      req.flush(mockJob);
    });

    it('should handle HTTP error response', () => {
      const jobId = 'invalid-id';
      const errorMessage = 'Job not found';

      service.getJobById(jobId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.GET_BY_ID(jobId));
      expect(req.request.method).toBe('GET');
      req.flush(errorMessage, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('createJob', () => {
    it('should create a new job and return it', () => {
      service.createJob(createJobRequest).subscribe((job) => {
        expect(job).toEqual(mockJob);
        expect(job.jobType).toBe(createJobRequest.jobType);
        expect(job.status).toBe(JobStatus.Pending);
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.CREATE);
      expect(req.request.method).toBe('POST');
      expect(req.request.url).toBe(JOBS_ENDPOINTS.CREATE);
      expect(req.request.body).toEqual(createJobRequest);
      expect(req.request.headers.get('Content-Type')).toBe('application/json');
      req.flush(mockJob);
    });

    it('should handle validation error when creating job', () => {
      const invalidRequest: CreateJobRequestDto = {
        jobType: '', // Invalid empty job type
      };

      service.createJob(invalidRequest).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(400);
          expect(error.statusText).toBe('Bad Request');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.CREATE);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(invalidRequest);
      req.flush('Validation failed', {
        status: 400,
        statusText: 'Bad Request',
      });
    });

    it('should send correct headers when creating job', () => {
      service.createJob(createJobRequest).subscribe();

      const req = httpMock.expectOne(JOBS_ENDPOINTS.CREATE);
      expect(req.request.headers.get('Content-Type')).toBe('application/json');
      req.flush(mockJob);
    });
  });

  describe('endpoint construction', () => {
    it('should construct correct endpoint URLs', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';

      expect(JOBS_ENDPOINTS.GET_BY_ID(jobId)).toContain(`/jobs/${jobId}`);
      expect(JOBS_ENDPOINTS.CREATE).toContain('/jobs');
    });
  });
});
