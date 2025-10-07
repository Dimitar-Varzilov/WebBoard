import {
  Component,
  EventEmitter,
  Input,
  Output,
  OnDestroy,
  OnInit,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { JobDto, JobStatus } from '../../../models';
import {
  JobService,
  ReportService,
  SignalRService,
  JobStatusUpdate,
} from '../../../services';
import { JobModelFactory } from '../../../factories/model.factory';
import { DateTimeUtils } from '../../../utils/datetime.utils';

@Component({
  selector: 'app-job-detail',
  templateUrl: './job-detail.component.html',
  styleUrls: ['./job-detail.component.scss'],
})
export class JobDetailComponent implements OnInit, OnChanges, OnDestroy {
  @Input() job?: JobDto | null;
  @Output() close = new EventEmitter<void>();

  JobStatus = JobStatus;
  refreshing = false;
  isDownloading = false;

  private destroy$ = new Subject<void>();
  private currentJobId?: string;

  constructor(
    private jobService: JobService,
    private reportService: ReportService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    this.subscribeToJobUpdates();
    this.subscribeToSpecificJob();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When the job input changes, update SignalR subscription
    if (changes['job']) {
      this.subscribeToSpecificJob();
    }
  }

  ngOnDestroy(): void {
    // Unsubscribe from the specific job
    if (this.currentJobId) {
      this.signalRService.unsubscribeFromJob(this.currentJobId);
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Subscribe to general job status updates from SignalR
   */
  private subscribeToJobUpdates(): void {
    this.signalRService
      .getJobStatusUpdates()
      .pipe(takeUntil(this.destroy$))
      .subscribe((update) => {
        if (!update || !this.job) return;

        // Only update if this is the job we're displaying
        if (update.jobId === this.job.id) {
          console.log('📨 Job detail received update for job:', update.jobId);
          this.updateJobFromSignalR(update);
        }
      });
  }

  /**
   * Subscribe to updates for the specific job being displayed
   */
  private async subscribeToSpecificJob(): Promise<void> {
    // Unsubscribe from previous job if any
    if (this.currentJobId && this.currentJobId !== this.job?.id) {
      await this.signalRService.unsubscribeFromJob(this.currentJobId);
    }

    // Subscribe to the new job
    if (this.job?.id && this.signalRService.isConnected()) {
      this.currentJobId = this.job.id;
      await this.signalRService.subscribeToJob(this.job.id);
      console.log(`✅ Job detail subscribed to job ${this.job.id}`);
    }
  }

  /**
   * Update the displayed job with data from SignalR
   */
  private updateJobFromSignalR(update: JobStatusUpdate): void {
    if (!this.job) return;

    // Create updated raw job data
    const rawJob = {
      ...this.job,
      status: update.status,
      hasReport: update.hasReport || this.job.hasReport,
      reportId: update.reportId || this.job.reportId,
      reportFileName: update.reportFileName || this.job.reportFileName,
    };

    // Use factory to recompute all properties
    this.job = JobModelFactory.fromApiResponse(rawJob);

    console.log(
      `🔄 Job detail updated from SignalR - Status: ${this.getStatusText(
        update.status
      )}`
    );
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

  onClose(): void {
    this.close.emit();
  }
}
