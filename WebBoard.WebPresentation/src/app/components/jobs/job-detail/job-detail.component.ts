import {
  Component,
  EventEmitter,
  Input,
  Output,
  OnDestroy,
} from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { JobDto, JobStatus } from '../../../models';
import { JobService, ReportService } from '../../../services';
import { DateTimeUtils } from '../../../utils/datetime.utils';

@Component({
  selector: 'app-job-detail',
  templateUrl: './job-detail.component.html',
  styleUrls: ['./job-detail.component.scss'],
})
export class JobDetailComponent implements OnDestroy {
  @Input() job?: JobDto | null;
  @Output() edit = new EventEmitter<JobDto>();
  @Output() delete = new EventEmitter<JobDto>();
  @Output() close = new EventEmitter<void>();

  JobStatus = JobStatus;
  refreshing = false;
  isDownloading = false;

  private destroy$ = new Subject<void>();

  constructor(
    private jobService: JobService,
    private reportService: ReportService
  ) {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getStatusClass(status: JobStatus): string {
    switch (status) {
      case JobStatus.Queued:
        return 'job-status-pending';
      case JobStatus.Running:
        return 'job-status-running';
      case JobStatus.Completed:
        return 'job-status-completed';
      case JobStatus.Failed:
        return 'job-status-failed';
      default:
        return 'job-status-pending';
    }
  }

  getStatusText(status: JobStatus): string {
    switch (status) {
      case JobStatus.Queued:
        return 'Queued';
      case JobStatus.Running:
        return 'Running';
      case JobStatus.Completed:
        return 'Completed';
      case JobStatus.Failed:
        return 'Failed';
      default:
        return 'Queued';
    }
  }

  getProgressClass(status: JobStatus): string {
    switch (status) {
      case JobStatus.Queued:
        return 'bg-secondary';
      case JobStatus.Running:
        return 'bg-warning';
      case JobStatus.Completed:
        return 'bg-success';
      case JobStatus.Failed:
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }

  getProgressPercentage(status: JobStatus): number {
    switch (status) {
      case JobStatus.Queued:
        return 25;
      case JobStatus.Running:
        return 75;
      case JobStatus.Completed:
        return 100;
      case JobStatus.Failed:
        return 100;
      default:
        return 0;
    }
  }

  /**
   * Get current timezone info - cached as it doesn't change
   */
  get timezoneInfo(): string {
    return `${DateTimeUtils.getCurrentTimezoneName()} (${DateTimeUtils.getCurrentTimezoneOffset()})`;
  }

  onRefresh(): void {
    if (!this.job) {
      return;
    }
    this.refreshing = true;
    this.jobService
      .getJobById(this.job.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedJob) => {
          this.job = updatedJob;
          this.refreshing = false;
        },
        error: (error) => {
          console.error('Error refreshing job:', error);
          this.refreshing = false;
        },
      });
  }

  onDownloadReport(): void {
    if (!this.job?.reportId || this.isDownloading) {
      return;
    }

    this.isDownloading = true;

    this.reportService
      .downloadReport(this.job.reportId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          // Trigger automatic download
          const fileName = this.job?.reportFileName || 'report.txt';
          this.reportService.triggerDownload(blob, fileName);
          this.isDownloading = false;
        },
        error: (error) => {
          console.error('Error downloading report:', error);
          alert('Failed to download report. Please try again.');
          this.isDownloading = false;
        },
      });
  }

  /**
   * Check if job is in a non-editable state (not queued)
   */
  isJobNonEditable(): boolean {
    return this.job?.status !== JobStatus.Queued;
  }

  /**
   * Check if job can be edited (only queued jobs can be edited)
   */
  canEdit(): boolean {
    if (!this.job) return false;
    return this.job.status === JobStatus.Queued;
  }

  /**
   * Check if job can be deleted (only queued jobs can be deleted)
   */
  canDelete(): boolean {
    if (!this.job) return false;
    return this.job.status === JobStatus.Queued;
  }

  onEdit(): void {
    if (!this.job || !this.canEdit()) {
      return;
    }
    this.close.emit(); // Close detail modal first
    this.edit.emit(this.job); // Then emit edit event
  }

  onDelete(): void {
    if (!this.job || !this.canDelete()) {
      return;
    }
    this.delete.emit(this.job);
  }

  onClose(): void {
    this.close.emit();
  }
}
