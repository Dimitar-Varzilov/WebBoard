import { Component, EventEmitter, Input, Output } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';
import { JobService, ReportService } from '../../../services';

@Component({
  selector: 'app-job-detail',
  templateUrl: './job-detail.component.html',
  styleUrls: ['./job-detail.component.scss'],
})
export class JobDetailComponent {
  @Input() job?: JobDto | null;
  @Output() close = new EventEmitter<void>();

  JobStatus = JobStatus;
  refreshing = false;
  isDownloading = false;

  constructor(
    private jobService: JobService,
    private reportService: ReportService
  ) {}

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

  onRefresh(): void {
    if (!this.job) {
      return;
    }
    this.refreshing = true;
    this.jobService.getJobById(this.job.id).subscribe({
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

    this.reportService.downloadReport(this.job.reportId).subscribe({
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
