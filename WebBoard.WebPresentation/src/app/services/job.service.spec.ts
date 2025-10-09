import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { JobService } from './job.service';
import {
  JobDto,
  CreateJobRequestDto,
  UpdateJobRequestDto,
  JobStatus,
} from '../models';
import { JOBS_ENDPOINTS } from '../constants/endpoints';

describe('JobService', () => {
  let service: JobService;
  let httpMock: HttpTestingController;

  const mockJob: JobDto = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    jobType: 'DataProcessing',
    status: JobStatus.Queued,
    createdAt: '2024-01-01T00:00:00Z',
    createdAtDate: new Date('2024-01-01T00:00:00Z'),
    createdAtDisplay: 'Jan 1, 2024',
    createdAtRelative: '1 day ago',
    createdAtCompact: '1/1/24',
    isScheduledInPast: false,
    isOverdue: false,
    taskCount: 0,
  };

  const createJobRequest: CreateJobRequestDto = {
    jobType: 'DataProcessing',
    taskIds: ['123e4567-e89b-12d3-a456-426614174000'],
  };

  const updateJobRequest: UpdateJobRequestDto = {
    jobType: 'DataProcessing',
    runImmediately: false,
    scheduledAt: '2024-01-15T10:00:00Z',
    taskIds: ['123e4567-e89b-12d3-a456-426614174000'],
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

  describe('getJobs', () => {
    it('should return paginated jobs', () => {
      const mockJobs: JobDto[] = [
        mockJob,
        {
          id: '123e4567-e89b-12d3-a456-426614174001',
          jobType: 'ReportGeneration',
          status: JobStatus.Running,
          createdAt: '2024-01-02T00:00:00Z',
          createdAtDate: new Date('2024-01-02T00:00:00Z'),
          createdAtDisplay: 'Jan 2, 2024',
          createdAtRelative: '2 days ago',
          createdAtCompact: '1/2/24',
          isScheduledInPast: false,
          isOverdue: false,
          taskCount: 0,
        },
      ];

      const mockPagedResult = {
        items: mockJobs,
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 2,
          totalPages: 1,
        },
      };

      service.getJobs({ pageNumber: 1, pageSize: 10 }).subscribe((result) => {
        expect(result.items.length).toBe(2);
        expect(result.items[0].jobType).toBe('DataProcessing');
        expect(result.items[1].jobType).toBe('ReportGeneration');
        expect(result.items[0].createdAtDate).toBeDefined();
        expect(result.metadata.totalCount).toBe(2);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(JOBS_ENDPOINTS.BASE)
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockPagedResult);
    });

    it('should return empty array when no jobs exist', () => {
      const emptyPagedResult = {
        items: [],
        metadata: {
          currentPage: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
        },
      };

      service.getJobs({ pageNumber: 1, pageSize: 10 }).subscribe((result) => {
        expect(result.items).toEqual([]);
        expect(result.items.length).toBe(0);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(JOBS_ENDPOINTS.BASE)
      );
      expect(req.request.method).toBe('GET');
      req.flush(emptyPagedResult);
    });

    it('should handle HTTP error when getting jobs', () => {
      service.getJobs({ pageNumber: 1, pageSize: 10 }).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Internal Server Error');
        },
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes(JOBS_ENDPOINTS.BASE)
      );
      req.flush('Server error', {
        status: 500,
        statusText: 'Internal Server Error',
      });
    });
  });

  describe('getJobById', () => {
    it('should return a job when valid id is provided', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';

      service.getJobById(jobId).subscribe((job) => {
        expect(job.id).toBe(jobId);
        expect(job.jobType).toBe('DataProcessing');
        expect(job.status).toBe(JobStatus.Queued);
        expect(job.createdAtDate).toBeDefined();
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
        expect(job.jobType).toBe(createJobRequest.jobType);
        expect(job.status).toBe(JobStatus.Queued);
        expect(job.createdAtDate).toBeDefined();
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.CREATE);
      expect(req.request.method).toBe('POST');
      expect(req.request.url).toBe(JOBS_ENDPOINTS.CREATE);
      expect(req.request.body).toEqual(createJobRequest);
      req.flush(mockJob);
    });

    it('should handle validation error when creating job', () => {
      const invalidRequest: CreateJobRequestDto = {
        jobType: '', // Invalid empty job type
        taskIds: [], // Empty task ids
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
  });

  describe('endpoint construction', () => {
    it('should construct correct endpoint URLs', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';

      expect(JOBS_ENDPOINTS.BASE).toContain('/jobs');
      expect(JOBS_ENDPOINTS.GET_BY_ID(jobId)).toContain(`/jobs/${jobId}`);
      expect(JOBS_ENDPOINTS.CREATE).toContain('/jobs');
    });
  });

  describe('updateJob', () => {
    it('should update a queued job and return it', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';
      const updatedJob: JobDto = {
        ...mockJob,
        scheduledAt: '2024-01-15T10:00:00Z',
        scheduledAtDate: new Date('2024-01-15T10:00:00Z'),
        scheduledAtDisplay: 'Jan 15, 2024',
        scheduledAtRelative: 'in 14 days',
        scheduledAtCompact: '1/15/24',
      };

      service.updateJob(jobId, updateJobRequest).subscribe((job) => {
        expect(job.id).toBe(jobId);
        expect(job.scheduledAt).toBe('2024-01-15T10:00:00Z');
        expect(job.scheduledAtDate).toBeDefined();
        expect(job.jobType).toBe('DataProcessing');
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.UPDATE(jobId));
      expect(req.request.method).toBe('PUT');
      expect(req.request.url).toBe(JOBS_ENDPOINTS.UPDATE(jobId));
      expect(req.request.body).toEqual(updateJobRequest);
      req.flush(updatedJob);
    });

    it('should handle error when updating non-queued job', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';
      const errorMessage = 'Only queued jobs can be updated';

      service.updateJob(jobId, updateJobRequest).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(409);
          expect(error.statusText).toBe('Conflict');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.UPDATE(jobId));
      expect(req.request.method).toBe('PUT');
      req.flush(
        { message: errorMessage },
        { status: 409, statusText: 'Conflict' }
      );
    });

    it('should handle validation error when updating job', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';
      const invalidRequest: UpdateJobRequestDto = {
        jobType: '',
        runImmediately: false,
        scheduledAt: undefined,
        taskIds: [],
      };

      service.updateJob(jobId, invalidRequest).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(400);
          expect(error.statusText).toBe('Bad Request');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.UPDATE(jobId));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(invalidRequest);
      req.flush('Validation failed', {
        status: 400,
        statusText: 'Bad Request',
      });
    });

    it('should handle not found error when updating non-existent job', () => {
      const jobId = 'non-existent-id';

      service.updateJob(jobId, updateJobRequest).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.UPDATE(jobId));
      expect(req.request.method).toBe('PUT');
      req.flush('Job not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('deleteJob', () => {
    it('should delete a queued job successfully', () => {
      const jobId = '123e4567-e89b12d3-a456-426614174000';

      service.deleteJob(jobId).subscribe((result) => {
        expect(result).toBeNull();
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.DELETE(jobId));
      expect(req.request.method).toBe('DELETE');
      expect(req.request.url).toBe(JOBS_ENDPOINTS.DELETE(jobId));
      req.flush(null);
    });

    it('should handle error when deleting non-queued job', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';
      const errorMessage = 'Only queued jobs can be deleted';

      service.deleteJob(jobId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(409);
          expect(error.statusText).toBe('Conflict');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.DELETE(jobId));
      expect(req.request.method).toBe('DELETE');
      req.flush(
        { message: errorMessage },
        { status: 409, statusText: 'Conflict' }
      );
    });

    it('should handle not found error when deleting non-existent job', () => {
      const jobId = 'non-existent-id';

      service.deleteJob(jobId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.DELETE(jobId));
      expect(req.request.method).toBe('DELETE');
      req.flush('Job not found', { status: 404, statusText: 'Not Found' });
    });

    it('should handle server error when deleting job', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174000';

      service.deleteJob(jobId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Internal Server Error');
        },
      });

      const req = httpMock.expectOne(JOBS_ENDPOINTS.DELETE(jobId));
      expect(req.request.method).toBe('DELETE');
      req.flush('Server error', {
        status: 500,
        statusText: 'Internal Server Error',
      });
    });
  });
});
