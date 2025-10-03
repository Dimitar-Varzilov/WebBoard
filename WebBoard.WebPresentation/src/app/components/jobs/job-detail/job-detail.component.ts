import { Component, EventEmitter, Input, Output } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';
import { JobService } from '../../../services';

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

  constructor(private jobService: JobService) {}

  getStatusClass(status: JobStatus): string {
    switch (status) {
      case JobStatus.Pending:
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
      case JobStatus.Pending:
        return 'Pending';
      case JobStatus.Running:
        return 'Running';
      case JobStatus.Completed:
        return 'Completed';
      case JobStatus.Failed:
        return 'Failed';
      default:
        return 'Pending';
    }
  }

  getProgressClass(status: JobStatus): string {
    switch (status) {
      case JobStatus.Pending:
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
      case JobStatus.Pending:
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

  onClose(): void {
    this.close.emit();
  }
}
