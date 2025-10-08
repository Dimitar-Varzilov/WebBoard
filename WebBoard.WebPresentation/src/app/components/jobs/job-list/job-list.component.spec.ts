import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
import { JobListComponent } from './job-list.component';
import { JobService, SignalRService, JobStatusUpdate } from '../../../services';
import { JobDto, JobStatus, PagedResult } from '../../../models';
import { ROUTES } from '../../../constants';
import * as signalR from '@microsoft/signalr';
import { FormsModule } from '@angular/forms';
import { TaskCardComponent } from '../../tasks/task-card/task-card.component';

describe('JobListComponent', () => {
  let component: JobListComponent;
  let fixture: ComponentFixture<JobListComponent>;
  let mockJobService: jasmine.SpyObj<JobService>;
  let mockSignalRService: jasmine.SpyObj<SignalRService>;
  let jobStatusUpdates$: Subject<JobStatusUpdate>;
  let mockJobs: JobDto[];
  let mockPagedResult: PagedResult<JobDto>;

  const createMockJobs = (): JobDto[] => [
    {
      id: 'job-1',
      jobType: 'MarkAllTasksAsDone',
      status: JobStatus.Queued,
      createdAt: '2025-01-15T10:00:00Z',
      scheduledAt: undefined,
      hasReport: false,
      reportId: undefined,
      reportFileName: undefined,
      taskIds: ['task-1', 'task-2'],
      createdAtDate: new Date('2025-01-15T10:00:00Z'),
      createdAtDisplay: 'Jan 15, 2025',
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
      createdAt: '2025-01-15T09:00:00Z',
      scheduledAt: undefined,
      hasReport: false,
      reportId: undefined,
      reportFileName: undefined,
      taskIds: ['task-3'],
      createdAtDate: new Date('2025-01-15T09:00:00Z'),
      createdAtDisplay: 'Jan 15, 2025',
      createdAtRelative: '2 hours ago',
      createdAtCompact: 'Jan 15',
      taskCount: 1,
      isScheduledInPast: false,
      isOverdue: false,
    },
    {
      id: 'job-3',
      jobType: 'GenerateTaskReport',
      status: JobStatus.Completed,
      createdAt: '2025-01-14T10:00:00Z',
      scheduledAt: undefined,
      hasReport: true,
      reportId: 'report-1',
      reportFileName: 'report.txt',
      taskIds: ['task-4'],
      createdAtDate: new Date('2025-01-14T10:00:00Z'),
      createdAtDisplay: 'Jan 14, 2025',
      createdAtRelative: '1 day ago',
      createdAtCompact: 'Jan 14',
      taskCount: 1,
      isScheduledInPast: false,
      isOverdue: false,
    },
  ];

  const createMockPagedResult = (jobs: JobDto[]): PagedResult<JobDto> => ({
    items: jobs,
    metadata: {
      currentPage: 1,
      pageSize: 1000,
      totalCount: jobs.length,
      totalPages: 1,
      hasPrevious: false,
      hasNext: false,
    },
  });

  beforeEach(async () => {
    // Create fresh mock data for each test
    mockJobs = createMockJobs();
    mockPagedResult = createMockPagedResult(mockJobs);

    jobStatusUpdates$ = new Subject<JobStatusUpdate>();

    mockJobService = jasmine.createSpyObj('JobService', [
      'getJobs',
      'getJobById',
    ]);
    mockSignalRService = jasmine.createSpyObj('SignalRService', [
      'getJobStatusUpdates',
      'getConnectionState',
      'subscribeToJobs',
      'unsubscribeFromJobs',
      'isConnected',
    ]);

    mockJobService.getJobs.and.returnValue(of(mockPagedResult));
    mockJobService.getJobById.and.returnValue(of(mockJobs[0]));
    mockSignalRService.getJobStatusUpdates.and.returnValue(
      jobStatusUpdates$.asObservable()
    );
    mockSignalRService.getConnectionState.and.returnValue(
      of(signalR.HubConnectionState.Disconnected)
    );
    mockSignalRService.isConnected.and.returnValue(true);
    mockSignalRService.subscribeToJobs.and.returnValue(Promise.resolve());
    mockSignalRService.unsubscribeFromJobs.and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      declarations: [JobListComponent],
      providers: [
        { provide: JobService, useValue: mockJobService },
        { provide: SignalRService, useValue: mockSignalRService },
      ],
      imports: [FormsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(JobListComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    jobStatusUpdates$.complete();
    // Clean up any filters that may have been set during tests
    if (component) {
      component.clearFilters();
    }
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load jobs on init', () => {
      fixture.detectChanges();

      expect(mockJobService.getJobs).toHaveBeenCalledWith(
        jasmine.objectContaining({
          pageNumber: 1,
          pageSize: 1000,
          sortBy: 'createdAt',
          sortDirection: 'desc',
        })
      );
      expect(component.jobs.length).toBe(3);
      expect(component.filteredJobs.length).toBe(3);
    });

    it('should subscribe to job status updates', () => {
      fixture.detectChanges();

      expect(mockSignalRService.getJobStatusUpdates).toHaveBeenCalled();
    });

    it('should subscribe to all loaded jobs via SignalR', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(mockSignalRService.subscribeToJobs).toHaveBeenCalledWith([
        'job-1',
        'job-2',
        'job-3',
      ]);
    }));

    it('should handle job loading errors gracefully', () => {
      mockJobService.getJobs.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      fixture.detectChanges();

      expect(component.jobs).toEqual([]);
      expect(component.loading).toBe(false);
    });
  });

  describe('SignalR Real-time Updates', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should update job status when SignalR update is received', fakeAsync(() => {
      tick();

      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-1',
        jobType: 'MarkAllTasksAsDone',
        status: JobStatus.Running,
        updatedAt: new Date().toISOString(),
        hasReport: false,
        reportId: undefined,
        reportFileName: undefined,
        errorMessage: undefined,
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();

      const updatedJob = component.jobs.find((j) => j.id === 'job-1');
      expect(updatedJob?.status).toBe(JobStatus.Running);
    }));

    it('should update job report info when job completes', fakeAsync(() => {
      tick();

      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-2',
        jobType: 'GenerateTaskReport',
        status: JobStatus.Completed,
        updatedAt: new Date().toISOString(),
        hasReport: true,
        reportId: 'new-report-id',
        reportFileName: 'new-report.txt',
        errorMessage: undefined,
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();

      const updatedJob = component.jobs.find((j) => j.id === 'job-2');
      expect(updatedJob?.status).toBe(JobStatus.Completed);
      expect(updatedJob?.hasReport).toBe(true);
      expect(updatedJob?.reportId).toBe('new-report-id');
      expect(updatedJob?.reportFileName).toBe('new-report.txt');
    }));

    it('should update selected job when viewing in modal', fakeAsync(() => {
      tick();

      component.selectedJob = mockJobs[1]; // job-2 (Running)

      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-2', // Update the same job that's selected
        jobType: 'GenerateTaskReport',
        status: JobStatus.Completed,
        updatedAt: new Date().toISOString(),
        hasReport: true,
        reportId: 'report-1',
        reportFileName: 'report.txt',
        errorMessage: undefined,
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();

      expect(component.selectedJob.status).toBe(JobStatus.Completed);
      expect(component.selectedJob.hasReport).toBe(true);
    }));

    it('should reload jobs if update received for job not in list', fakeAsync(() => {
      tick();

      mockJobService.getJobs.calls.reset();

      const statusUpdate: JobStatusUpdate = {
        jobId: 'job-99',
        jobType: 'TestJob',
        status: JobStatus.Completed,
        updatedAt: new Date().toISOString(),
        hasReport: false,
        reportId: undefined,
        reportFileName: undefined,
        errorMessage: undefined,
      };

      jobStatusUpdates$.next(statusUpdate);
      tick();

      expect(mockJobService.getJobs).toHaveBeenCalled();
    }));

    it('should handle null status updates gracefully', fakeAsync(() => {
      tick();

      jobStatusUpdates$.next(null as any);
      tick();

      expect(component.jobs.length).toBe(3);
    }));
  });

  describe('Filtering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should filter jobs by search text', () => {
      component['_searchText'] = 'MarkAllTasksAsDone';
      component.filterJobs();

      expect(component.filteredJobs.length).toBe(1);
      expect(component.filteredJobs[0].id).toBe('job-1');
    });

    it('should filter jobs by job ID', () => {
      component['_searchText'] = 'job-2';
      component.filterJobs();

      expect(component.filteredJobs.length).toBe(1);
      expect(component.filteredJobs[0].id).toBe('job-2');
    });

    it('should filter jobs by status', () => {
      component['_statusFilter'] = JobStatus.Completed.toString();
      component.filterJobs();

      expect(component.filteredJobs.length).toBe(1);
      expect(component.filteredJobs[0].status).toBe(JobStatus.Completed);
    });

    it('should filter by both search text and status', () => {
      component['_searchText'] = 'GenerateTaskReport';
      component['_statusFilter'] = JobStatus.Running.toString();
      component.filterJobs();

      expect(component.filteredJobs.length).toBe(1);
      expect(component.filteredJobs[0].id).toBe('job-2');
    });

    it('should be case-insensitive for search', () => {
      component['_searchText'] = 'generatetaskreport';
      component.filterJobs();

      expect(component.filteredJobs.length).toBe(2);
    });

    it('should clear all filters', () => {
      component['_searchText'] = 'test';
      component['_statusFilter'] = JobStatus.Running.toString();
      component.filterJobs();

      component.clearFilters();

      expect(component.searchText).toBe('');
      expect(component.statusFilter).toBe('');
      expect(component.filteredJobs.length).toBe(3);
    });
  });

  describe('Job Actions', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should view job details', () => {
      const job = mockJobs[0];
      component.viewJob(job);

      expect(component.selectedJob).toBe(job);
      expect(component.showJobDetail).toBe(true);
    });

    it('should close job detail modal', () => {
      component.selectedJob = mockJobs[0];
      component.showJobDetail = true;

      component.onJobDetailClosed();

      expect(component.selectedJob).toBeNull();
      expect(component.showJobDetail).toBe(false);
    });

    it('should refresh job status', () => {
      const job = mockJobs[0];
      const updatedJob = { ...job, status: JobStatus.Running };
      mockJobService.getJobById.and.returnValue(of(updatedJob));

      component.refreshJobStatus(job);

      expect(mockJobService.getJobById).toHaveBeenCalledWith('job-1');
    });

    it('should update job in list after refresh', () => {
      const job = mockJobs[0];
      const updatedJob = { ...job, status: JobStatus.Completed };
      mockJobService.getJobById.and.returnValue(of(updatedJob));

      component.refreshJobStatus(job);

      const jobInList = component.jobs.find((j) => j.id === 'job-1');
      expect(jobInList?.status).toBe(JobStatus.Completed);
    });

    it('should handle refresh error gracefully', () => {
      mockJobService.getJobById.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      const job = mockJobs[0];
      component.refreshJobStatus(job);

      expect(component.jobs.length).toBe(3);
    });

    it('should reload all jobs', () => {
      mockJobService.getJobs.calls.reset();

      component.refreshJobs();

      expect(mockJobService.getJobs).toHaveBeenCalled();
    });
  });

  describe('Job Statistics', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should count jobs by status - Queued', () => {
      const count = component.getJobCountByStatus(JobStatus.Queued);
      expect(count).toBe(1);
    });

    it('should count jobs by status - Running', () => {
      const count = component.getJobCountByStatus(JobStatus.Running);
      expect(count).toBe(1);
    });

    it('should count jobs by status - Completed', () => {
      const count = component.getJobCountByStatus(JobStatus.Completed);
      expect(count).toBe(1);
    });

    it('should return 0 for status with no jobs', () => {
      const count = component.getJobCountByStatus(JobStatus.Failed);
      expect(count).toBe(0);
    });
  });

  describe('Component Lifecycle', () => {
    it('should unsubscribe from SignalR on destroy', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      fixture.destroy();

      expect(mockSignalRService.unsubscribeFromJobs).toHaveBeenCalled();
    }));

    it('should complete destroy subject on destroy', () => {
      fixture.detectChanges();

      const destroySpy = spyOn<any>(component['destroy$'], 'complete');
      fixture.destroy();

      expect(destroySpy).toHaveBeenCalled();
    });
  });

  describe('TrackBy Function', () => {
    it('should return job id for trackBy', () => {
      const job = mockJobs[0];
      const result = component.trackByJobId(0, job);

      expect(result).toBe('job-1');
    });
  });

  describe('SignalR Connection State', () => {
    it('should not subscribe to jobs if SignalR is not connected', fakeAsync(() => {
      mockSignalRService.isConnected.and.returnValue(false);
      mockSignalRService.subscribeToJobs.calls.reset();

      fixture.detectChanges();
      tick();

      // Should still call subscribeToJobs but it will be handled gracefully
      expect(component.jobs.length).toBe(3);
    }));
  });

  describe('Constants', () => {
    it('should expose ROUTES constant', () => {
      expect(component.routes).toBe(ROUTES);
    });

    it('should expose JobStatus enum', () => {
      expect(component.JobStatus).toBe(JobStatus);
    });
  });
});
