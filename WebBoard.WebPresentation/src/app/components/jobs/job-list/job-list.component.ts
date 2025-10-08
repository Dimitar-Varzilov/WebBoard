import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { JobDto, JobStatus, JobQueryParameters } from '../../../models';
import { JobService, JobStatusUpdate, SignalRService } from '../../../services';
import { JobModelFactory } from '../../../factories/model.factory';
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

  private _searchText = '';
  get searchText(): string {
    return this._searchText;
  }
  set searchText(value: string) {
    this._searchText = value;
    this.filterJobs();
  }

  private _statusFilter = '';
  get statusFilter(): string {
    return this._statusFilter;
  }
  set statusFilter(value: string) {
    this._statusFilter = value;
    this.filterJobs();
  }

  routes = ROUTES;
  JobStatus = JobStatus;

  // Modal states
  showJobDetail = false;
  selectedJob: JobDto | null = null;

  // Track subscribed job IDs
  private subscribedJobIds: Set<string> = new Set();
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
    // Unsubscribe from all jobs
    this.unsubscribeFromAllJobs();
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

          // Update selectedJob reference if this is the job being viewed in the modal
          // This ensures the modal displays the latest data without needing its own SignalR subscription
          if (this.selectedJob && this.selectedJob.id === update.jobId) {
            this.selectedJob = this.jobs[jobIndex];
            console.log('📨 Updated selectedJob reference for modal display');
          }

          this.filterJobs();

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
        next: async (result) => {
          this.jobs = result.items;
          this.filterJobs();
          this.loading = false;

          // Subscribe to all loaded jobs using batch operation
          await this.subscribeToAllJobs();
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
    // Initialize filteredJobs to empty if jobs not loaded yet
    if (!this.jobs) {
      this.filteredJobs = [];
      return;
    }

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
    this._searchText = '';
    this._statusFilter = '';
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
}
