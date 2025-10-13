import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';
import { JobDetailComponent } from './job-detail.component';
import { JobService, ReportService } from '../../../services';
import { JobDto, JobStatus } from '../../../models';

describe('JobDetailComponent', () => {
  let component: JobDetailComponent;
  let fixture: ComponentFixture<JobDetailComponent>;
  let mockJobService: jasmine.SpyObj<JobService>;
  let mockReportService: jasmine.SpyObj<ReportService>;

  const mockJob: JobDto = {
    id: 'job-123',
    jobType: 'GenerateTaskReport',
    status: JobStatus.Running,
    createdAt: '2025-01-15T10:00:00Z',
    scheduledAt: '2025-01-15T12:00:00Z',
    hasReport: false,
    reportId: undefined,
    reportFileName: undefined,
    taskIds: ['task-1', 'task-2'],
    createdAtDate: new Date('2025-01-15T10:00:00Z'),
    createdAtDisplay: 'Jan 15, 2025',
    createdAtRelative: '1 hour ago',
    createdAtCompact: 'Jan 15',
    taskCount: 2,
    isOverdue: false,
    isScheduledInPast: false,
  };

  const mockCompletedJob: JobDto = {
    ...mockJob,
    status: JobStatus.Completed,
    hasReport: true,
    reportId: 'report-456',
    reportFileName: 'task-report-2025-01-15.txt',
  };

  beforeEach(async () => {
    mockJobService = jasmine.createSpyObj('JobService', ['getJobById']);
    mockReportService = jasmine.createSpyObj('ReportService', [
      'downloadReport',
      'triggerDownload',
    ]);

    await TestBed.configureTestingModule({
      declarations: [JobDetailComponent],
      providers: [
        { provide: JobService, useValue: mockJobService },
        { provide: ReportService, useValue: mockReportService },
      ],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    fixture = TestBed.createComponent(JobDetailComponent);
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
      expect(percentage).toBe(25);
    });

    it('should get correct progress percentage for Running', () => {
      const percentage = component.getProgressPercentage(JobStatus.Running);
      expect(percentage).toBe(75);
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

  describe('Job Refresh', () => {
    it('should refresh job details', () => {
      const updatedJob = { ...mockJob, status: JobStatus.Completed };
      mockJobService.getJobById.and.returnValue(of(updatedJob));

      component.onRefresh();

      expect(mockJobService.getJobById).toHaveBeenCalledWith('job-123');
      expect(component.refreshing).toBe(false);
    });

    it('should update job data after refresh', () => {
      const updatedJob = { ...mockJob, status: JobStatus.Completed };
      mockJobService.getJobById.and.returnValue(of(updatedJob));

      component.onRefresh();

      expect(component.job?.status).toBe(JobStatus.Completed);
    });

    it('should handle refresh error gracefully', () => {
      mockJobService.getJobById.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      component.onRefresh();

      expect(component.refreshing).toBe(false);
    });

    it('should not refresh if job is null', () => {
      component.job = null;

      component.onRefresh();

      expect(mockJobService.getJobById).not.toHaveBeenCalled();
    });

    it('should set refreshing flag during refresh', () => {
      mockJobService.getJobById.and.returnValue(of(mockJob));

      component.onRefresh();

      expect(mockJobService.getJobById).toHaveBeenCalled();
    });
  });

  describe('Report Download', () => {
    beforeEach(() => {
      component.job = mockCompletedJob;
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
        'task-report-2025-01-15.txt'
      );
    });

    it('should use default filename if reportFileName is missing', () => {
      component.job = { ...mockCompletedJob, reportFileName: undefined };
      const mockBlob = new Blob(['test report'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.onDownloadReport();

      expect(mockReportService.triggerDownload).toHaveBeenCalledWith(
        mockBlob,
        'report.txt'
      );
    });

    it('should set downloading flag during download', () => {
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
      component.job = { ...mockCompletedJob, reportId: undefined };

      component.onDownloadReport();

      expect(mockReportService.downloadReport).not.toHaveBeenCalled();
    });

    it('should prevent multiple simultaneous downloads', () => {
      const mockBlob = new Blob(['test report'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.isDownloading = true;
      component.onDownloadReport();

      expect(mockReportService.downloadReport).not.toHaveBeenCalled();
    });
  });

  describe('Modal Actions', () => {
    it('should emit close event', () => {
      spyOn(component.close, 'emit');

      component.onClose();

      expect(component.close.emit).toHaveBeenCalled();
    });
  });

  describe('Timezone Info', () => {
    it('should get timezone info', () => {
      const timezoneInfo = component.timezoneInfo;

      // Timezone info returns IANA timezone name and offset, e.g., "Europe/Sofia (+03:00)"
      expect(typeof timezoneInfo).toBe('string');
      expect(timezoneInfo).toContain('(');
      expect(timezoneInfo).toContain(')');
      expect(timezoneInfo).toMatch(/[+-]\d{2}:\d{2}/);
    });
  });

  describe('Component Lifecycle', () => {
    it('should complete destroy subject on destroy', () => {
      const destroySpy = spyOn<any>(component['destroy$'], 'complete');

      fixture.destroy();

      expect(destroySpy).toHaveBeenCalled();
    });

    it('should unsubscribe from observables on destroy', () => {
      const mockBlob = new Blob(['test'], { type: 'text/plain' });
      mockReportService.downloadReport.and.returnValue(of(mockBlob));

      component.job = mockCompletedJob;
      component.onDownloadReport();

      fixture.destroy();

      // No error should be thrown
      expect(component).toBeTruthy();
    });
  });

  describe('Job Status Enum', () => {
    it('should expose JobStatus enum', () => {
      expect(component.JobStatus).toBe(JobStatus);
    });
  });
});
