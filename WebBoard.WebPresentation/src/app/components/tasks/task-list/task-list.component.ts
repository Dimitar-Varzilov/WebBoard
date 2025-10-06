import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  TaskDto,
  TaskItemStatus,
  TaskQueryParameters,
  PAGE_SIZES,
  DEFAULT_PAGE_SIZE,
} from '../../../models';
import { TaskService } from '../../../services';
import { PaginationFactory } from '../../../services/pagination-factory.service';
import { PaginatedDataService } from '../../../services/paginated-data.service';
import { ROUTES, TASK_STATUS_OPTIONS } from '../../../constants';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
})
export class TaskListComponent implements OnInit, OnDestroy {
  // Pagination service
  paginationService!: PaginatedDataService<TaskDto, TaskQueryParameters>;

  searchText = '';
  statusFilter = '';
  taskStatusOptions = TASK_STATUS_OPTIONS;
  pageSizes = PAGE_SIZES;

  // Expose Math for template
  Math = Math;

  // Modal states
  showTaskForm = false;
  showTaskDetail = false;
  selectedTask: TaskDto | null = null;
  isEditMode = false;

  private destroy$ = new Subject<void>();

  constructor(
    private taskService: TaskService,
    private paginationFactory: PaginationFactory,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Create pagination service with default params
    this.paginationService = this.paginationFactory.create<
      TaskDto,
      TaskQueryParameters
    >((params) => this.taskService.getTasks(params), {
      pageSize: DEFAULT_PAGE_SIZE,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    });

    // Subscribe to state for error handling
    this.paginationService.state$.pipe(takeUntil(this.destroy$)).subscribe({
      error: (error) => console.error('Pagination error:', error),
    });

    // Initial load
    this.refreshTasks();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Convenience getters for template
  get filteredTasks() {
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

  refreshTasks(): void {
    this.paginationService.refresh();
  }

  filterTasks(): void {
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

  createTask(): void {
    this.router.navigate([ROUTES.TASK_CREATE]);
  }

  editTask(task: TaskDto): void {
    this.selectedTask = task;
    this.isEditMode = true;
    this.showTaskForm = true;
  }

  viewTask(task: TaskDto): void {
    this.selectedTask = task;
    this.showTaskDetail = true;
  }

  deleteTask(task: TaskDto): void {
    if (confirm(`Are you sure you want to delete the task "${task.title}"?`)) {
      this.taskService.deleteTask(task.id).subscribe({
        next: () => {
          this.refreshTasks();
        },
        error: (error) => {
          console.error('Error deleting task:', error);
          alert('Failed to delete task. Please try again.');
        },
      });
    }
  }

  onTaskSaved(task: TaskDto): void {
    this.showTaskForm = false;
    this.selectedTask = null;
    this.refreshTasks();
  }

  onTaskFormCanceled(): void {
    this.showTaskForm = false;
    this.selectedTask = null;
  }

  onTaskDetailClosed(): void {
    this.showTaskDetail = false;
    this.selectedTask = null;
  }

  trackByTaskId(index: number, task: TaskDto): string {
    return task.id;
  }
}
