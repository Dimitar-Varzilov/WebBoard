import { Component, EventEmitter, Input, Output } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';

@Component({
  selector: 'app-job-card',
  templateUrl: './job-card.component.html',
  styleUrls: ['./job-card.component.scss'],
})
export class JobCardComponent {
  @Input() job!: JobDto;
  @Output() view = new EventEmitter<JobDto>();
  @Output() refresh = new EventEmitter<JobDto>();

  isRefreshing = false;

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
        return 0;
      case JobStatus.Running:
        return 50;
      case JobStatus.Completed:
        return 100;
      case JobStatus.Failed:
        return 100;
      default:
        return 0;
    }
  }

  onView(): void {
    this.view.emit(this.job);
  }

  onRefresh(): void {
    this.isRefreshing = true;
    this.refresh.emit(this.job);

    // Stop spinning after a short delay
    setTimeout(() => {
      this.isRefreshing = false;
    }, 1000);
  }
}
