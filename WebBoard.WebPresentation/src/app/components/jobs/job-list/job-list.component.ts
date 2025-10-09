import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import {
  JobDto,
  JobStatus,
  JobQueryParameters,
  PAGE_SIZES,
  DEFAULT_PAGE_SIZE,
} from '../../../models';
import { JobService, JobStatusUpdate, SignalRService } from '../../../services';
import { PaginationFactory } from '../../../services/pagination-factory.service';
import { PaginatedDataService } from '../../../services/paginated-data.service';
import { JobModelFactory } from '../../../factories/model.factory';
import { ROUTES } from '../../../constants';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss'],
})
export class JobListComponent implements OnInit, OnDestroy {
  // Pagination service
  paginationService!: PaginatedDataService<JobDto, JobQueryParameters>;

  jobs: JobDto[] = [];
  pageSizes = PAGE_SIZES;

  // Expose Math for template
  Math = Math;

  routes = ROUTES;
  JobStatus = JobStatus;

  // Modal states
  showJobDetail = false;
  showJobForm = false;
  selectedJob: JobDto | null = null;
  isEditMode = false;

  // Track subscribed job IDs
  private subscribedJobIds: Set<string> = new Set();
  private destroy$ = new Subject<void>();

  constructor(
    private jobService: JobService,
    private signalRService: SignalRService,
    private paginationFactory: PaginationFactory
  ) {}

  ngOnInit(): void {
    // Create pagination service with default params and page size of 10
    this.paginationService = this.paginationFactory.create<
      JobDto,
      JobQueryParameters
    >((params) => this.jobService.getJobs(params), {
      pageSize: 10,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    });

    // Subscribe to pagination state for job updates
    this.paginationService.state$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (state) => {
        // Update local jobs array for statistics and SignalR subscriptions
        this.jobs = state.data;
        this.subscribeToAllJobs();
      },
      error: (error) => console.error('Pagination error:', error),
    });

    this.subscribeToJobUpdates();
    this.loadJobs();
  }

  ngOnDestroy(): void {
    // Unsubscribe from all jobs
    this.unsubscribeFromAllJobs();
    this.destroy$.next();
    this.destroy$.complete();
    // Cleanup pagination service to prevent memory leaks
    if (this.paginationService) {
      this.paginationService.destroy();
    }
  }

  // Convenience getters for template
  get filteredJobs() {
    return this.paginationService.getData();
  }

  get loading() {
    return this.paginationService.isLoading();
  }

  get paginationMetadata() {
    return this.paginationService.getMetadata();
  }

  get currentPage() {
    return this.paginationService.getCurrentParams().pageNumber || 1;
  }

  get pageSize() {
    return (
      this.paginationService.getCurrentParams().pageSize || DEFAULT_PAGE_SIZE
    );
  }

  set pageSize(value: number) {
    this.onPageSizeChange(value);
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

          // Update selectedJob reference if this is the job being viewed in the modal
          // This ensures the modal displays the latest data without needing its own SignalR subscription
          if (this.selectedJob && this.selectedJob.id === update.jobId) {
            this.selectedJob = this.jobs[jobIndex];
            console.log('📨 Updated selectedJob reference for modal display');
          }

          // Refresh pagination to update the display
          this.paginationService.refresh();

          // Show notification
          this.showNotification(update);
        } else {
          // Job not in list - might be a newly created job
          console.log('📥 Received update for job not in list, refreshing...');
          this.loadJobs();
        }
      });
  }

  /**
   * Subscribe to all jobs currently in the list using batch operation
   */
  private async subscribeToAllJobs(): Promise<void> {
    if (!this.signalRService.isConnected()) {
      console.warn('⚠️ Cannot subscribe - SignalR not connected');
      return;
    }

    const jobIds = this.jobs.map((job) => job.id);

    // Find new jobs that haven't been subscribed to yet
    const newJobIds = jobIds.filter((id) => !this.subscribedJobIds.has(id));

    if (newJobIds.length === 0) {
      console.log('✅ No new jobs to subscribe to');
      return;
    }

    // Use batch subscription for better performance
    await this.signalRService.subscribeToJobs(newJobIds);

    // Track subscribed jobs
    newJobIds.forEach((id) => this.subscribedJobIds.add(id));

    console.log(
      `✅ Subscribed to ${this.subscribedJobIds.size} total jobs (${newJobIds.length} new)`
    );
  }

  /**
   * Unsubscribe from all jobs using batch operation
   */
  private async unsubscribeFromAllJobs(): Promise<void> {
    if (this.subscribedJobIds.size === 0) {
      return;
    }

    const jobIdsArray = Array.from(this.subscribedJobIds);

    // Use batch unsubscription for better performance
    await this.signalRService.unsubscribeFromJobs(jobIdsArray);

    this.subscribedJobIds.clear();
    console.log('✅ Unsubscribed from all jobs');
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
    this.paginationService.refresh();
  }

  refreshJobs(): void {
    this.loadJobs();
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
            this.paginationService.refresh();
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

  editJob(job: JobDto): void {
    this.selectedJob = job;
    this.isEditMode = true;
    this.showJobForm = true;
  }

  deleteJob(job: JobDto): void {
    if (job.status !== JobStatus.Queued) {
      alert('Only queued jobs can be deleted.');
      return;
    }

    if (confirm(`Are you sure you want to delete the job "${job.jobType}"?`)) {
      this.jobService
        .deleteJob(job.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadJobs();
          },
          error: (error) => {
            console.error('Error deleting job:', error);
            if (error.status === 409 || error.status === 400) {
              alert(
                error.error?.message ||
                  'Cannot delete a non-queued job. Only queued jobs can be deleted.'
              );
            } else {
              alert('Failed to delete job. Please try again.');
            }
          },
        });
    }
  }

  onJobSaved(job: JobDto): void {
    this.showJobForm = false;
    this.selectedJob = null;
    this.loadJobs();
  }

  onJobFormCanceled(): void {
    this.showJobForm = false;
    this.selectedJob = null;
  }

  // Pagination methods
  onPageChange(page: number): void {
    this.paginationService.setPage(page);
  }

  onPageSizeChange(size: number): void {
    this.paginationService.setPageSize(size);
  }

  get visiblePages(): number[] {
    const metadata = this.paginationMetadata;
    if (!metadata) return [];
    const total = metadata.totalPages;
    const current = metadata.currentPage;
    const delta = 2; // Pages to show on each side of current page

    const range: number[] = [];
    for (
      let i = Math.max(2, current - delta);
      i <= Math.min(total - 1, current + delta);
      i++
    ) {
      range.push(i);
    }

    const pages: number[] = [];
    if (range.length > 0) {
      if (range[0] > 2) {
        pages.push(1, -1); // -1 represents ellipsis
      } else {
        pages.push(1);
      }

      pages.push(...range);

      if (range[range.length - 1] < total - 1) {
        pages.push(-1, total);
      } else if (total > 1) {
        pages.push(total);
      }
    } else {
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
    }

    return pages;
  }

  trackByJobId(index: number, job: JobDto): string {
    return job.id;
  }
}
