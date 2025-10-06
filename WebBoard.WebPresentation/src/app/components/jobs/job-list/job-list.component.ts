import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  JobDto,
  JobStatus,
  JobQueryParameters,
  PAGE_SIZES,
  DEFAULT_PAGE_SIZE,
} from '../../../models';
import { JobService } from '../../../services';
import { PaginationFactory } from '../../../services/pagination-factory.service';
import { PaginatedDataService } from '../../../services/paginated-data.service';
import { DateTimeUtils } from '../../../utils/datetime.utils';
import { TIMING, ROUTES } from '../../../constants';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss'],
})
export class JobListComponent implements OnInit, OnDestroy {
  // Pagination service
  paginationService!: PaginatedDataService<JobDto, JobQueryParameters>;

  searchText = '';
  statusFilter = '';
  pageSizes = PAGE_SIZES;
  routes = ROUTES;
  JobStatus = JobStatus;

  // Modal states
  showJobDetail = false;
  selectedJob: JobDto | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private jobService: JobService,
    private paginationFactory: PaginationFactory
  ) {}

  ngOnInit(): void {
    // Create pagination service with default params
    this.paginationService = this.paginationFactory.create<
      JobDto,
      JobQueryParameters
    >((params) => this.jobService.getJobs(params), {
      pageSize: DEFAULT_PAGE_SIZE,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    });

    // Subscribe to state for error handling
    this.paginationService.state$.pipe(takeUntil(this.destroy$)).subscribe({
      error: (error) => console.error('Pagination error:', error),
    });

    // Initial load
    this.refreshJobs();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Convenience getters for template
  get jobs() {
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

  refreshJobs(): void {
    this.paginationService.refresh();
  }

  // Pagination methods
  onPageChange(page: number): void {
    this.paginationService.setPage(page);
  }

  onPageSizeChange(size: number): void {
    this.paginationService.setPageSize(size);
  }

  onSortChange(sortBy: string): void {
    const currentParams = this.paginationService.getCurrentParams();
    const newDirection =
      currentParams.sortBy === sortBy && currentParams.sortDirection === 'asc'
        ? 'desc'
        : 'asc';
    this.paginationService.setSort(sortBy, newDirection);
  }

  filterJobs(): void {
    this.paginationService.updateParams({
      searchTerm: this.searchText || undefined,
      status: this.statusFilter ? parseInt(this.statusFilter) : undefined,
    });
  }

  clearFilters(): void {
    this.searchText = '';
    this.statusFilter = '';
    this.paginationService.updateParams({
      searchTerm: undefined,
      status: undefined,
    });
  }

  get pages(): number[] {
    const metadata = this.paginationMetadata;
    if (!metadata) return [];
    return Array.from({ length: metadata.totalPages }, (_, i) => i + 1);
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
