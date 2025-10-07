import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { JobDto, JobStatus, JobQueryParameters } from '../../../models';
import { JobService, JobStatusUpdate, SignalRService } from '../../../services';
import { JobModelFactory } from '../../../factories/model.factory';
import { DateTimeUtils } from '../../../utils/datetime.utils';
import { ROUTES } from '../../../constants';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss'],
})
export class JobListComponent implements OnInit, OnDestroy {
  jobs: JobDto[] = [];
  filteredJobs: JobDto[] = [];
  loading = false;
  searchText = '';
  statusFilter = '';
  routes = ROUTES;
  JobStatus = JobStatus;
  DateTimeUtils = DateTimeUtils;

  // Modal states
  showJobDetail = false;
  selectedJob: JobDto | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private jobService: JobService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    this.loadJobs();
    this.subscribeToJobUpdates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeToJobUpdates(): void {
    this.signalRService
      .getJobStatusUpdates()
      .pipe(takeUntil(this.destroy$))
      .subscribe((update) => {
        if (!update) return;

        console.log(
          '📨 Job status update received in JobListComponent:',
          update
        );

        // Find and update the job in the list
        const jobIndex = this.jobs.findIndex((j) => j.id === update.jobId);
        if (jobIndex !== -1) {
          // Create updated raw job data
          const rawJob = {
            ...this.jobs[jobIndex],
            status: update.status,
            hasReport: update.hasReport || this.jobs[jobIndex].hasReport,
            reportId: update.reportId || this.jobs[jobIndex].reportId,
            reportFileName:
              update.reportFileName || this.jobs[jobIndex].reportFileName,
          };

          // Use factory to recompute all properties
          this.jobs[jobIndex] = JobModelFactory.fromApiResponse(rawJob);
          this.filterJobs();

          // Show notification
          this.showNotification(update);
        }
      });
  }

  private showNotification(update: JobStatusUpdate): void {
    const statusText = this.getStatusText(update.status);
    const message = update.errorMessage
      ? `Job failed: ${update.errorMessage}`
      : `Job status: ${statusText}`;

    console.log(`🔔 Notification: ${message}`);
  }

  private getStatusText(status: JobStatus): string {
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
        return 'Unknown';
    }
  }

  loadJobs(): void {
    this.loading = true;

    // Backend only supports paginated queries
    const params: JobQueryParameters = {
      pageNumber: 1,
      pageSize: 1000, // Large page size to get all jobs
      sortBy: 'createdAt',
      sortDirection: 'desc',
    };

    this.jobService
      .getJobs(params)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.jobs = result.items;
          this.filterJobs();
          this.loading = false;
        },
        error: (error: any) => {
          console.error('Error loading jobs:', error);
          this.loading = false;
        },
      });
  }

  refreshJobs(): void {
    this.loadJobs();
  }

  filterJobs(): void {
    this.filteredJobs = this.jobs.filter((job) => {
      const matchesSearch =
        !this.searchText ||
        job.jobType.toLowerCase().includes(this.searchText.toLowerCase()) ||
        job.id.toLowerCase().includes(this.searchText.toLowerCase());

      const matchesStatus =
        !this.statusFilter || job.status.toString() === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  clearFilters(): void {
    this.searchText = '';
    this.statusFilter = '';
    this.filterJobs();
  }

  viewJob(job: JobDto): void {
    this.selectedJob = job;
    this.showJobDetail = true;
  }

  refreshJobStatus(job: JobDto): void {
    this.jobService
      .getJobById(job.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedJob: JobDto) => {
          const index = this.jobs.findIndex((j) => j.id === job.id);
          if (index !== -1) {
            this.jobs[index] = updatedJob;
            this.filterJobs();
          }
        },
        error: (error: any) => {
          console.error('Error refreshing job status:', error);
        },
      });
  }

  getJobCountByStatus(status: JobStatus): number {
    return this.jobs.filter((job) => job.status === status).length;
  }

  onJobDetailClosed(): void {
    this.showJobDetail = false;
    this.selectedJob = null;
  }

  trackByJobId(index: number, job: JobDto): string {
    return job.id;
  }

  /**
   * Format job creation time for display
   */
  formatCreatedAt(createdAt: string): string {
    return DateTimeUtils.formatCompact(createdAt);
  }

  /**
   * Format job scheduled time for display
   */
  formatScheduledAt(scheduledAt: string | undefined): string {
    if (!scheduledAt) return '';
    return DateTimeUtils.formatCompact(scheduledAt);
  }

  /**
   * Get relative time for job creation/scheduling
   */
  getRelativeTime(dateString: string): string {
    return DateTimeUtils.formatRelative(dateString);
  }

  /**
   * Check if scheduled time is in the past
   */
  isScheduledInPast(scheduledAt: string | undefined): boolean {
    if (!scheduledAt) return false;
    return DateTimeUtils.isPast(scheduledAt);
  }
}
