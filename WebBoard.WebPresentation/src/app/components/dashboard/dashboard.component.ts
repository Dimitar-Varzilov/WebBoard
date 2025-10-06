import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import {
  TaskDto,
  JobDto,
  TaskItemStatus,
  JobStatus,
  TaskQueryParameters,
  JobQueryParameters,
  PagedResult,
} from '../../models';
import { TaskService, JobService } from '../../services';
import { JOB_TYPES, ROUTES } from '../../constants';

interface TaskStats {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
}

interface JobStats {
  total: number;
  queued: number;
  running: number;
  completed: number;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  loading = false;
  routes = ROUTES;

  taskStats: TaskStats = {
    total: 0,
    pending: 0,
    inProgress: 0,
    completed: 0,
  };

  jobStats: JobStats = {
    total: 0,
    queued: 0,
    running: 0,
    completed: 0,
  };

  recentTasks: TaskDto[] = [];
  recentJobs: JobDto[] = [];

  constructor(
    private taskService: TaskService,
    private jobService: JobService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;

    // Load recent tasks (first page, sorted by creation date)
    const taskParams: TaskQueryParameters = {
      pageNumber: 1,
      pageSize: 5, // Get top 5 recent tasks for dashboard
      sortBy: 'createdAt',
      sortDirection: 'desc',
    };

    this.taskService.getTasks(taskParams).subscribe({
      next: (result: PagedResult<TaskDto>) => {
        this.recentTasks = result.items;
        this.calculateTaskStats(result.metadata.totalCount, result.items);
        this.checkLoadingComplete();
      },
      error: (error: Error) => {
        console.error('Error loading tasks:', error);
        this.checkLoadingComplete();
      },
    });

    // Load recent jobs (first page, sorted by creation date)
    const jobParams: JobQueryParameters = {
      pageNumber: 1,
      pageSize: 5, // Get top 5 recent jobs for dashboard
      sortBy: 'createdAt',
      sortDirection: 'desc',
    };

    this.jobService.getJobs(jobParams).subscribe({
      next: (result: PagedResult<JobDto>) => {
        this.recentJobs = result.items;
        this.calculateJobStats(result.metadata.totalCount, result.items);
        this.checkLoadingComplete();
      },
      error: (error: Error) => {
        console.error('Error loading jobs:', error);
        this.checkLoadingComplete();
      },
    });
  }

  private checkLoadingComplete(): void {
    // Set loading to false after both calls complete (success or error)
    this.loading = false;
  }

  private calculateTaskStats(totalCount: number, recentItems: TaskDto[]): void {
    this.taskStats = {
      total: totalCount,
      pending: recentItems.filter((t) => t.status === TaskItemStatus.Pending)
        .length,
      inProgress: recentItems.filter(
        (t) => t.status === TaskItemStatus.InProgress
      ).length,
      completed: recentItems.filter(
        (t) => t.status === TaskItemStatus.Completed
      ).length,
    };
  }

  private calculateJobStats(totalCount: number, recentItems: JobDto[]): void {
    this.jobStats = {
      total: totalCount,
      queued: recentItems.filter((j) => j.status === JobStatus.Queued).length,
      running: recentItems.filter((j) => j.status === JobStatus.Running).length,
      completed: recentItems.filter((j) => j.status === JobStatus.Completed)
        .length,
    };
  }

  getPercentage(value: number, total: number): number {
    return total > 0 ? (value / total) * 100 : 0;
  }

  getTaskStatusClass(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.Pending:
        return 'status-pending';
      case TaskItemStatus.InProgress:
        return 'status-in-progress';
      case TaskItemStatus.Completed:
        return 'status-completed';
      default:
        return 'status-pending';
    }
  }

  getTaskStatusText(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.Pending:
        return 'Pending';
      case TaskItemStatus.InProgress:
        return 'In Progress';
      case TaskItemStatus.Completed:
        return 'Completed';
      default:
        return 'Pending';
    }
  }

  getJobStatusClass(status: JobStatus): string {
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

  getJobStatusText(status: JobStatus): string {
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

  createNewTask(): void {
    this.router.navigate([ROUTES.TASK_CREATE]);
  }

  createJob(): void {
    this.router.navigate([ROUTES.JOB_CREATE]);
  }

  refreshData(): void {
    this.loadDashboardData();
  }
}
