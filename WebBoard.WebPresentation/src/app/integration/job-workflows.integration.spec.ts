import {
  TestBed,
  ComponentFixture,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FormsModule } from '@angular/forms';
import { of, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

import { JobListComponent } from '../components/jobs/job-list/job-list.component';
import { JobDetailComponent } from '../components/jobs/job-detail/job-detail.component';
import { JobCardComponent } from '../components/jobs/job-card/job-card.component';

import { JobService } from '../services/job.service';
import { ReportService } from '../services/report.service';
import { SignalRService, JobStatusUpdate } from '../services/signalr.service';
import { JOBS_ENDPOINTS, REPORTS_ENDPOINTS } from '../constants/endpoints';
import { JobDto, JobStatus } from '../models/job.model';

describe('Job Workflows E2E Integration Tests', () => {
  let jobService: JobService;
  let reportService: ReportService;
  let signalRService: jasmine.SpyObj<SignalRService>;
  let httpMock: HttpTestingController;

  // Mock data
  const mockJobs: JobDto[] = [
    {
      id: 'job-1',
      jobType: 'MarkAllTasksAsDone',
      status: JobStatus.Queued,
      createdAt: '2024-01-15T10:00:00Z',
      scheduledAt: undefined,
      hasReport: false,
      reportId: undefined,
      reportFileName: undefined,
      taskIds: ['task-1', 'task-2'],
      createdAtDate: new Date('2024-01-15T10:00:00Z'),
      createdAtDisplay: 'Jan 15, 2024',
      createdAtRelative: '1 hour ago',
      createdAtCompact: 'Jan 15',
      taskCount: 2,
      isScheduledInPast: false,
      isOverdue: false,
    },
    {
      id: 'job-2',
      jobType: 'GenerateTaskReport',
      status: JobStatus.Running,
      createdAt: '2024-01-15T11:00:00Z',
      scheduledAt: undefined,
      hasReport: false,
      reportId: undefined,
      reportFileName: undefined,
      taskIds: ['task-3'],
      createdAtDate: new Date('2024-01-15T11:00:00Z'),
      createdAtDisplay: 'Jan 15, 2024',
      createdAtRelative: '30 min ago',
      createdAtCompact: 'Jan 15',
      taskCount: 1,
      isScheduledInPast: false,
      isOverdue: false,
    },
    {
      id: 'job-3',
      jobType: 'GenerateTaskReport',
      status: JobStatus.Completed,
      createdAt: '2024-01-15T09:00:00Z',
      scheduledAt: undefined,
      hasReport: true,
      reportId: 'report-123',
      reportFileName: 'report.txt',
      taskIds: ['task-4'],
      createdAtDate: new Date('2024-01-15T09:00:00Z'),
      createdAtDisplay: 'Jan 15, 2024',
      createdAtRelative: '3 hours ago',
      createdAtCompact: 'Jan 15',
      taskCount: 1,
      isScheduledInPast: false,
      isOverdue: false,
    },
  ];

  const jobStatusUpdates$ = new Subject<JobStatusUpdate>();

  beforeEach(async () => {
    const signalRSpy = jasmine.createSpyObj('SignalRService', [
      'subscribeToJobs',
      'unsubscribeFromJobs',
      'connect',
      'disconnect',
      'getJobStatusUpdates',
      'getConnectionState',
      'isConnected',
    ]);
    signalRSpy.getJobStatusUpdates.and.returnValue(
      jobStatusUpdates$.asObservable()
    );
    signalRSpy.getConnectionState.and.returnValue(
      of(signalR.HubConnectionState.Disconnected)
    );
    signalRSpy.isConnected.and.returnValue(true);

    await TestBed.configureTestingModule({
      declarations: [JobListComponent, JobDetailComponent, JobCardComponent],
      imports: [HttpClientTestingModule, RouterTestingModule, FormsModule],
      providers: [
        JobService,
        ReportService,
        { provide: SignalRService, useValue: signalRSpy },
      ],
    }).compileComponents();

    jobService = TestBed.inject(JobService);
    reportService = TestBed.inject(ReportService);
    signalRService = TestBed.inject(
      SignalRService
    ) as jasmine.SpyObj<SignalRService>;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Complete Job Workflow: List → Detail → Download Report', () => {
    let listFixture: ComponentFixture<JobListComponent>;
    let listComponent: JobListComponent;
    let detailFixture: ComponentFixture<JobDetailComponent>;
    let detailComponent: JobDetailComponent;

    beforeEach(() => {
      listFixture = TestBed.createComponent(JobListComponent);
      listComponent = listFixture.componentInstance;
    });

    it('should load job list, view details, and download report', fakeAsync(async () => {
      // Step 1: Load job list
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      expect(jobRequest.request.method).toBe('GET');
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      expect(listComponent.jobs.length).toBe(3);

      // Step 2: View job details
      const completedJob = mockJobs[2]; // Job with report
      listComponent.viewJob(completedJob);

      tick();
      listFixture.detectChanges();

      expect(listComponent.selectedJob).toEqual(completedJob);

      // Step 3: Create detail component with selected job
      detailFixture = TestBed.createComponent(JobDetailComponent);
      detailComponent = detailFixture.componentInstance;
      detailComponent.job = completedJob;
      detailFixture.detectChanges();

      expect(detailComponent.job?.id).toBe('job-3');
      expect(detailComponent.job?.status).toBe(JobStatus.Completed);
      expect(detailComponent.job?.reportId).toBe('report-123');

      // Step 4: Download report
      spyOn(reportService, 'triggerDownload');
      detailComponent.onDownloadReport();

      const reportRequest = httpMock.expectOne(
        REPORTS_ENDPOINTS.DOWNLOAD(completedJob.reportId!)
      );
      expect(reportRequest.request.method).toBe('GET');
      expect(reportRequest.request.responseType).toBe('blob');

      const mockBlob = new Blob(['report content'], {
        type: 'application/pdf',
      });
      reportRequest.flush(mockBlob);

      tick();

      expect(reportService.triggerDownload).toHaveBeenCalledWith(
        jasmine.any(Blob),
        completedJob.reportFileName || 'report.txt'
      );
      expect(detailComponent.isDownloading).toBe(false);
    }));

    it('should handle report download error gracefully', fakeAsync(() => {
      // Setup
      listFixture.detectChanges();

      const initialRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      initialRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();

      const completedJob = mockJobs[2];
      detailFixture = TestBed.createComponent(JobDetailComponent);
      detailComponent = detailFixture.componentInstance;
      detailComponent.job = completedJob;
      detailFixture.detectChanges();

      spyOn(window, 'alert');

      // Download with error
      detailComponent.onDownloadReport();

      const reportRequest = httpMock.expectOne(
        REPORTS_ENDPOINTS.DOWNLOAD(completedJob.reportId!)
      );
      reportRequest.error(new ErrorEvent('Network error'));

      tick();

      expect(window.alert).toHaveBeenCalled();
      expect(detailComponent.isDownloading).toBe(false);
    }));
  });

  describe('Filter Jobs → View Filtered Job → Verify Details', () => {
    let listFixture: ComponentFixture<JobListComponent>;
    let listComponent: JobListComponent;

    beforeEach(() => {
      listFixture = TestBed.createComponent(JobListComponent);
      listComponent = listFixture.componentInstance;
    });

    it('should filter jobs by status and view filtered job', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      expect(listComponent.jobs.length).toBe(3);

      // Filter by Completed status
      listComponent.statusFilter = String(JobStatus.Completed);
      listFixture.detectChanges();

      const filteredJobs = listComponent.filteredJobs;
      expect(filteredJobs.length).toBe(1);
      expect(filteredJobs[0].status).toBe(JobStatus.Completed);

      // View filtered job
      listComponent.viewJob(filteredJobs[0]);
      tick();

      expect(listComponent.selectedJob).toEqual(filteredJobs[0]);
      expect(listComponent.selectedJob?.id).toBe('job-3');
      expect(listComponent.selectedJob?.jobType).toBe('GenerateTaskReport');
    }));

    it('should filter jobs by search text', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      // Search by jobType
      listComponent.searchText = 'GenerateTaskReport';
      listFixture.detectChanges();

      const filteredJobs = listComponent.filteredJobs;
      expect(filteredJobs.length).toBe(2);
      expect(filteredJobs[0].jobType).toContain('GenerateTaskReport');
      expect(filteredJobs[0].status).toBe(JobStatus.Running);
    }));

    it('should combine search and status filters', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      // Apply both filters
      listComponent.searchText = 'GenerateTaskReport';
      listComponent.statusFilter = String(JobStatus.Running);
      listFixture.detectChanges();

      const filteredJobs = listComponent.filteredJobs;
      expect(filteredJobs.length).toBe(1);
      expect(filteredJobs[0].jobType).toBe('GenerateTaskReport');
      expect(filteredJobs[0].status).toBe(JobStatus.Running);
    }));
  });

  describe('SignalR Real-Time Updates → UI Updates', () => {
    let listFixture: ComponentFixture<JobListComponent>;
    let listComponent: JobListComponent;

    beforeEach(() => {
      listFixture = TestBed.createComponent(JobListComponent);
      listComponent = listFixture.componentInstance;
    });

    it('should update job status in list when SignalR update received', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      const queuedJob = listComponent.jobs.find((j) => j.id === 'job-1');
      expect(queuedJob?.status).toBe(JobStatus.Queued);

      // Simulate SignalR update
      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-1',
        jobType: 'MarkAllTasksAsDone',
        status: JobStatus.Running,
        updatedAt: new Date().toISOString(),
        taskCount: 15,
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();
      listFixture.detectChanges();

      const runningJob = listComponent.jobs.find((j) => j.id === 'job-1');
      expect(runningJob?.status).toBe(JobStatus.Running);
    }));

    it('should update selected job when SignalR update received', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      // Select a job
      const runningJob = mockJobs[1];
      listComponent.viewJob(runningJob);
      tick();

      expect(listComponent.selectedJob?.status).toBe(JobStatus.Running);

      // Simulate SignalR update
      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-2',
        jobType: 'GenerateTaskReport',
        status: JobStatus.Running,
        updatedAt: new Date().toISOString(),
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();
      listFixture.detectChanges();

      expect(listComponent.selectedJob?.status).toBe(JobStatus.Running);
    }));

    it('should update job report when completed via SignalR', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      const runningJob = listComponent.jobs.find((j) => j.id === 'job-2');
      expect(runningJob?.reportId).toBeUndefined();

      // Simulate job completion with report
      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-2',
        jobType: 'GenerateTaskReport',
        status: JobStatus.Completed,
        updatedAt: new Date().toISOString(),
        hasReport: true,
        reportId: 'report-456',
        reportFileName: 'report.txt',
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();
      listFixture.detectChanges();

      const updatedJob = listComponent.jobs.find((j) => j.id === 'job-2');
      expect(updatedJob?.status).toBe(JobStatus.Completed);
      expect(updatedJob?.reportId).toBe('report-456');
    }));
  });

  describe('Job Statistics and Counts', () => {
    let listFixture: ComponentFixture<JobListComponent>;
    let listComponent: JobListComponent;

    beforeEach(() => {
      listFixture = TestBed.createComponent(JobListComponent);
      listComponent = listFixture.componentInstance;
    });

    it('should calculate job counts by status', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      expect(listComponent.getJobCountByStatus(JobStatus.Queued)).toBe(1);
      expect(listComponent.getJobCountByStatus(JobStatus.Running)).toBe(1);
      expect(listComponent.getJobCountByStatus(JobStatus.Completed)).toBe(1);
      expect(listComponent.getJobCountByStatus(JobStatus.Failed)).toBe(0);
    }));

    it('should update counts when jobs change status via SignalR', fakeAsync(() => {
      // Load jobs
      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();
      listFixture.detectChanges();

      expect(listComponent.getJobCountByStatus(JobStatus.Queued)).toBe(1);
      expect(listComponent.getJobCountByStatus(JobStatus.Running)).toBe(1);

      // Update queued job to running
      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-1',
        jobType: 'MarkAllTasksAsDone',
        status: JobStatus.Running,
        updatedAt: new Date().toISOString(),
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();
      listFixture.detectChanges();

      expect(listComponent.getJobCountByStatus(JobStatus.Queued)).toBe(0);
      expect(listComponent.getJobCountByStatus(JobStatus.Running)).toBe(2);
    }));
  });

  describe('Job Refresh Functionality', () => {
    let detailFixture: ComponentFixture<JobDetailComponent>;
    let detailComponent: JobDetailComponent;

    beforeEach(() => {
      detailFixture = TestBed.createComponent(JobDetailComponent);
      detailComponent = detailFixture.componentInstance;
      detailComponent.job = mockJobs[1]; // Running job
      detailFixture.detectChanges();
    });

    it('should refresh job details on demand', fakeAsync(() => {
      expect(detailComponent.job?.status).toBe(JobStatus.Running);

      detailComponent.onRefresh();

      const refreshRequest = httpMock.expectOne(
        JOBS_ENDPOINTS.GET_BY_ID(detailComponent.job!.id)
      );
      expect(refreshRequest.request.method).toBe('GET');

      const updatedJob: JobDto = {
        ...mockJobs[1],
        status: JobStatus.Completed,
      };
      refreshRequest.flush(updatedJob);

      tick();

      expect(detailComponent.job?.status).toBe(JobStatus.Completed);
      expect(detailComponent.refreshing).toBe(false);
    }));

    it('should handle refresh errors gracefully', fakeAsync(() => {
      spyOn(console, 'error');

      detailComponent.onRefresh();

      const refreshRequest = httpMock.expectOne(
        JOBS_ENDPOINTS.GET_BY_ID(detailComponent.job!.id)
      );
      refreshRequest.error(new ErrorEvent('Network error'));

      tick();

      expect(console.error).toHaveBeenCalled();
      expect(detailComponent.refreshing).toBe(false);
    }));
  });

  describe('Job List Refresh and Reload', () => {
    let listFixture: ComponentFixture<JobListComponent>;
    let listComponent: JobListComponent;

    beforeEach(() => {
      listFixture = TestBed.createComponent(JobListComponent);
      listComponent = listFixture.componentInstance;
    });

    it('should reload all jobs', fakeAsync(() => {
      // Initial load
      listFixture.detectChanges();

      const initialRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      initialRequest.flush({ items: mockJobs, totalCount: 3 });

      tick();

      expect(listComponent.jobs.length).toBe(3);

      // Reload
      listComponent.loadJobs();

      const reloadRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      expect(reloadRequest.request.method).toBe('GET');

      const newMockJob: JobDto = {
        id: 'job-4',
        jobType: 'NewJobType',
        status: JobStatus.Queued,
        createdAt: new Date().toISOString(),
        scheduledAt: undefined,
        hasReport: false,
        reportId: undefined,
        reportFileName: undefined,
        taskIds: [],
        createdAtDate: new Date(),
        createdAtDisplay: 'Now',
        createdAtRelative: 'Just now',
        createdAtCompact: 'Now',
        taskCount: 0,
        isScheduledInPast: false,
        isOverdue: false,
      };
      const newMockJobs = [...mockJobs, newMockJob];
      reloadRequest.flush({ items: newMockJobs, totalCount: 4 });

      tick();

      expect(listComponent.jobs.length).toBe(4);
    }));
  });

  describe('Error Handling', () => {
    let listFixture: ComponentFixture<JobListComponent>;
    let listComponent: JobListComponent;

    beforeEach(() => {
      listFixture = TestBed.createComponent(JobListComponent);
      listComponent = listFixture.componentInstance;
    });

    it('should handle job list load error', fakeAsync(() => {
      spyOn(console, 'error');

      listFixture.detectChanges();

      const jobRequest = httpMock.expectOne(
        `${JOBS_ENDPOINTS.BASE}?pageNumber=1&pageSize=1000&sortBy=createdAt&sortDirection=desc`
      );
      jobRequest.error(new ErrorEvent('Network error'));

      tick();

      expect(console.error).toHaveBeenCalled();
      expect(listComponent.jobs.length).toBe(0);
    }));
  });
});
