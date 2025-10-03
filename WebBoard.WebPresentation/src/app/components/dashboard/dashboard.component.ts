import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { TaskDto, JobDto, TaskItemStatus, JobStatus } from '../../models';
import { TaskService, JobService } from '../../services';
import { JOB_TYPES, ROUTES } from '../../constants';

interface TaskStats {
  total: number;
  notStarted: number;
  inProgress: number;
  completed: number;
  onHold: number;
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
    notStarted: 0,
    inProgress: 0,
    completed: 0,
    onHold: 0,
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

    // Load tasks
    this.taskService.getAllTasks().subscribe({
      next: (tasks) => {
        this.recentTasks = tasks.sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
        this.calculateTaskStats(tasks);
        this.checkLoadingComplete();
      },
      error: (error) => {
        console.error('Error loading tasks:', error);
        this.checkLoadingComplete();
      },
    });

    // Load jobs
    this.jobService.getAllJobs().subscribe({
      next: (jobs) => {
        this.recentJobs = jobs.sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
        this.calculateJobStats(jobs);
        this.checkLoadingComplete();
      },
      error: (error) => {
        console.error('Error loading jobs:', error);
        this.checkLoadingComplete();
      },
    });
  }

  private checkLoadingComplete(): void {
    // Set loading to false after both calls complete (success or error)
    this.loading = false;
  }

  private calculateTaskStats(tasks: TaskDto[]): void {
    this.taskStats = {
      total: tasks.length,
      notStarted: tasks.filter((t) => t.status === TaskItemStatus.NotStarted)
        .length,
      inProgress: tasks.filter((t) => t.status === TaskItemStatus.InProgress)
        .length,
      completed: tasks.filter((t) => t.status === TaskItemStatus.Completed)
        .length,
      onHold: tasks.filter((t) => t.status === TaskItemStatus.OnHold).length,
    };
  }

  private calculateJobStats(jobs: JobDto[]): void {
    this.jobStats = {
      total: jobs.length,
      queued: jobs.filter((j) => j.status === JobStatus.Queued).length,
      running: jobs.filter((j) => j.status === JobStatus.Running).length,
      completed: jobs.filter((j) => j.status === JobStatus.Completed).length,
    };
  }

  getPercentage(value: number, total: number): number {
    return total > 0 ? (value / total) * 100 : 0;
  }

  getTaskStatusClass(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.NotStarted:
        return 'status-not-started';
      case TaskItemStatus.InProgress:
        return 'status-in-progress';
      case TaskItemStatus.Completed:
        return 'status-completed';
      case TaskItemStatus.OnHold:
        return 'status-on-hold';
      default:
        return 'status-not-started';
    }
  }

  getTaskStatusText(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.NotStarted:
        return 'Not Started';
      case TaskItemStatus.InProgress:
        return 'In Progress';
      case TaskItemStatus.Completed:
        return 'Completed';
      case TaskItemStatus.OnHold:
        return 'On Hold';
      default:
        return 'Not Started';
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
