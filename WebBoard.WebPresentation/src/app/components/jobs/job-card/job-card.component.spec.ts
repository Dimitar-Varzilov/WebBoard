import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError, BehaviorSubject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { JobCardComponent } from './job-card.component';
import { ReportService, SignalRService } from '../../../services';
import { JobDto, JobStatus } from '../../../models';
import { TIMING } from '../../../constants';

describe('JobCardComponent', () => {
  let component: JobCardComponent;
  let fixture: ComponentFixture<JobCardComponent>;
  let mockReportService: jasmine.SpyObj<ReportService>;
  let mockSignalRService: jasmine.SpyObj<SignalRService>;
  let connectionState$: BehaviorSubject<signalR.HubConnectionState>;

  const mockJob: JobDto = {
    id: 'job-123',
    jobType: 'MarkAllTasksAsDone',
    status: JobStatus.Running,
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
  };

  const mockJobWithReport: JobDto = {
    ...mockJob,
    status: JobStatus.Completed,
    hasReport: true,
    reportId: 'report-456',
    reportFileName: 'task-report.txt',
  };

  beforeEach(async () => {
    connectionState$ = new BehaviorSubject<signalR.HubConnectionState>(
      signalR.HubConnectionState.Connected
    );

    mockReportService = jasmine.createSpyObj('ReportService', [
      'downloadReport',
      'triggerDownload',
    ]);
    mockSignalRService = jasmine.createSpyObj('SignalRService', [
      'getConnectionState',
    ]);

    mockSignalRService.getConnectionState.and.returnValue(
      connectionState$.asObservable()
    );

    await TestBed.configureTestingModule({
      declarations: [JobCardComponent],
      providers: [
        { provide: ReportService, useValue: mockReportService },
        { provide: SignalRService, useValue: mockSignalRService },
      ],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    fixture = TestBed.createComponent(JobCardComponent);
    component = fixture.componentInstance;
    component.job = mockJob;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Status Display', () => {
    it('should get correct status class for Queued', () => {
      const statusClass = component.getStatusClass(JobStatus.Queued);
      expect(statusClass).toBe('job-status-pending');
    });

    it('should get correct status class for Running', () => {
      const statusClass = component.getStatusClass(JobStatus.Running);
      expect(statusClass).toBe('job-status-running');
    });

    it('should get correct status class for Completed', () => {
      const statusClass = component.getStatusClass(JobStatus.Completed);
      expect(statusClass).toBe('job-status-completed');
    });

    it('should get correct status class for Failed', () => {
      const statusClass = component.getStatusClass(JobStatus.Failed);
      expect(statusClass).toBe('job-status-failed');
    });

    it('should get correct status text for Queued', () => {
      const statusText = component.getStatusText(JobStatus.Queued);
      expect(statusText).toBe('Queued');
    });

    it('should get correct status text for Running', () => {
      const statusText = component.getStatusText(JobStatus.Running);
      expect(statusText).toBe('Running');
    });

    it('should get correct status text for Completed', () => {
      const statusText = component.getStatusText(JobStatus.Completed);
      expect(statusText).toBe('Completed');
    });

    it('should get correct status text for Failed', () => {
      const statusText = component.getStatusText(JobStatus.Failed);
      expect(statusText).toBe('Failed');
    });
  });

  describe('Progress Display', () => {
    it('should get correct progress class for Queued', () => {
      const progressClass = component.getProgressClass(JobStatus.Queued);
      expect(progressClass).toBe('bg-secondary');
    });

    it('should get correct progress class for Running', () => {
      const progressClass = component.getProgressClass(JobStatus.Running);
      expect(progressClass).toBe('bg-warning');
    });

    it('should get correct progress class for Completed', () => {
      const progressClass = component.getProgressClass(JobStatus.Completed);
      expect(progressClass).toBe('bg-success');
    });

    it('should get correct progress class for Failed', () => {
      const progressClass = component.getProgressClass(JobStatus.Failed);
      expect(progressClass).toBe('bg-danger');
    });

    it('should get correct progress percentage for Queued', () => {
      const percentage = component.getProgressPercentage(JobStatus.Queued);
      expect(percentage).toBe(0);
    });

    it('should get correct progress percentage for Running', () => {
      const percentage = component.getProgressPercentage(JobStatus.Running);
      expect(percentage).toBe(50);
    });

    it('should get correct progress percentage for Completed', () => {
      const percentage = component.getProgressPercentage(JobStatus.Completed);
      expect(percentage).toBe(100);
    });

    it('should get correct progress percentage for Failed', () => {
      const percentage = component.getProgressPercentage(JobStatus.Failed);
      expect(percentage).toBe(100);
    });
  });

  describe('View Event', () => {
    it('should emit view event with job', () => {
      spyOn(component.view, 'emit');

      component.onView();

      expect(component.view.emit).toHaveBeenCalledWith(mockJob);
    });
  });

  describe('Refresh Event', () => {
    it('should emit refresh event with job', fakeAsync(() => {
      spyOn(component.refresh, 'emit');

      component.onRefresh();

      expect(component.refresh.emit).toHaveBeenCalledWith(mockJob);
      expect(component.isRefreshing).toBe(true);

      tick(TIMING.REFRESH_SPINNER_DURATION);

      expect(component.isRefreshing).toBe(false);
    }));

    it('should set refreshing flag temporarily', fakeAsync(() => {
      component.onRefresh();

      expect(component.isRefreshing).toBe(true);

      tick(TIMING.REFRESH_SPINNER_DURATION);

      expect(component.isRefreshing).toBe(false);
    }));
  });

  describe('Report Download', () => {
    beforeEach(() => {
      component.job = mockJobWithReport;
    });

    it('should download report when job has report', () => {
      const mockBlob = new Blob(['test report'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.onDownloadReport();

      expect(mockReportService.downloadReport).toHaveBeenCalledWith(
        'report-456'
      );
    });

    it('should trigger file download after successful API call', () => {
      const mockBlob = new Blob(['test report'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.onDownloadReport();

      expect(mockReportService.triggerDownload).toHaveBeenCalledWith(
        mockBlob,
        'task-report.txt'
      );
    });

    it('should use default filename if reportFileName is missing', () => {
      component.job = { ...mockJobWithReport, reportFileName: undefined };
      const mockBlob = new Blob(['test report'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.onDownloadReport();

      expect(mockReportService.triggerDownload).toHaveBeenCalledWith(
        mockBlob,
        'report.txt'
      );
    });

    it('should reset downloading flag after successful download', () => {
      const mockBlob = new Blob(['test report'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.onDownloadReport();

      expect(component.isDownloading).toBe(false);
    });

    it('should handle download error with alert', () => {
      spyOn(window, 'alert');
      mockReportService.downloadReport.and.returnValue(
        throwError(() => new Error('Download failed'))
      );

      component.onDownloadReport();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to download report. Please try again.'
      );
      expect(component.isDownloading).toBe(false);
    });

    it('should not download if job has no report', () => {
      component.job = mockJob;

      component.onDownloadReport();

      expect(mockReportService.downloadReport).not.toHaveBeenCalled();
    });

    it('should not download if reportId is missing', () => {
      component.job = { ...mockJobWithReport, reportId: undefined };

      component.onDownloadReport();

      expect(mockReportService.downloadReport).not.toHaveBeenCalled();
    });

    it('should prevent multiple simultaneous downloads', () => {
      component.isDownloading = true;

      component.onDownloadReport();

      expect(mockReportService.downloadReport).not.toHaveBeenCalled();
    });
  });

  describe('SignalR Connection State', () => {
    it('should track SignalR connection state', (done) => {
      component.isConnected$.subscribe((isConnected) => {
        expect(isConnected).toBe(true);
        done();
      });
    });

    it('should update when SignalR disconnects', (done) => {
      connectionState$.next(signalR.HubConnectionState.Disconnected);

      component.isConnected$.subscribe((isConnected) => {
        expect(isConnected).toBe(false);
        done();
      });
    });

    it('should show connecting state', (done) => {
      connectionState$.next(signalR.HubConnectionState.Connecting);

      component.isConnected$.subscribe((isConnected) => {
        expect(isConnected).toBe(false);
        done();
      });
    });

    it('should show reconnecting state', (done) => {
      connectionState$.next(signalR.HubConnectionState.Reconnecting);

      component.isConnected$.subscribe((isConnected) => {
        expect(isConnected).toBe(false);
        done();
      });
    });
  });

  describe('Input Binding', () => {
    it('should accept job input', () => {
      const newJob: JobDto = {
        ...mockJob,
        id: 'job-999',
        status: JobStatus.Queued,
      };

      component.job = newJob;

      expect(component.job.id).toBe('job-999');
      expect(component.job.status).toBe(JobStatus.Queued);
    });
  });
});
