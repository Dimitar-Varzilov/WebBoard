import { Component, EventEmitter, Input, Output } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';
import { TIMING } from '../../../constants';

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
      case JobStatus.Queued:
        return 'job-status-pending';
      case JobStatus.Running:
        return 'job-status-running';
      case JobStatus.Completed:
        return 'job-status-completed';
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
      default:
        return 'bg-secondary';
    }
  }

  getProgressPercentage(status: JobStatus): number {
    switch (status) {
      case JobStatus.Queued:
        return 0;
      case JobStatus.Running:
        return 50;
      case JobStatus.Completed:
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
    }, TIMING.REFRESH_SPINNER_DURATION);
  }
}
