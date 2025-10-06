import { Component, OnInit, OnDestroy } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';
import { JobService } from '../../../services';
import { DateTimeUtils } from '../../../utils/datetime.utils';
import { TIMING, ROUTES } from '../../../constants';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss'],
})
export class JobListComponent implements OnInit {
  jobs: JobDto[] = [];
  filteredJobs: JobDto[] = [];
  loading = false;
  searchText = '';
  statusFilter = '';
  routes = ROUTES;
  JobStatus = JobStatus;
  DateTimeUtils = DateTimeUtils; // Make DateTimeUtils available in template

  // Modal states
  showJobDetail = false;
  selectedJob: JobDto | null = null;

  constructor(private jobService: JobService) {}

  ngOnInit(): void {
    this.loadJobs();
  }

  ngOnDestroy(): void {
    // Component cleanup
  }

  loadJobs(): void {
    this.loading = true;
    this.jobService.getAllJobs().subscribe({
      next: (jobs) => {
        this.jobs = jobs;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading jobs:', error);
        this.loading = false;
      },
    });
  }

  refreshJobs(): void {
    this.loadJobs();
  }

  viewJob(job: JobDto): void {
    this.selectedJob = job;
    this.showJobDetail = true;
  }

  refreshJobStatus(job: JobDto): void {
    this.jobService.getJobById(job.id).subscribe({
      next: (updatedJob) => {
        const index = this.jobs.findIndex((j) => j.id === job.id);
        if (index !== -1) {
          this.jobs[index] = updatedJob;
        }
      },
      error: (error) => {
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
