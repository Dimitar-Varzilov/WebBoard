import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TaskDto, JobDto, TaskItemStatus, JobStatus } from '../../models';
import { TaskService, JobService } from '../../services';

interface TaskStats {
  total: number;
  notStarted: number;
  inProgress: number;
  completed: number;
  onHold: number;
}

interface JobStats {
  total: number;
  pending: number;
  running: number;
  completed: number;
  failed: number;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  loading = false;

  taskStats: TaskStats = {
    total: 0,
    notStarted: 0,
    inProgress: 0,
    completed: 0,
    onHold: 0,
  };

  jobStats: JobStats = {
    total: 0,
    pending: 0,
    running: 0,
    completed: 0,
    failed: 0,
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
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading tasks:', error);
        this.loading = false;
      },
    });

    // Note: Since we don't have a getAllJobs endpoint, we'll simulate with empty array
    // In a real implementation, you would load jobs here too
    this.recentJobs = [];
    this.calculateJobStats([]);
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
      pending: jobs.filter((j) => j.status === JobStatus.Pending).length,
      running: jobs.filter((j) => j.status === JobStatus.Running).length,
      completed: jobs.filter((j) => j.status === JobStatus.Completed).length,
      failed: jobs.filter((j) => j.status === JobStatus.Failed).length,
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

  getJobStatusText(status: JobStatus): string {
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

  createNewTask(): void {
    this.router.navigate(['/tasks']);
  }

  markAllTasksDone(): void {
    this.jobService.createJob({ jobType: 'MarkAllTasksAsDone' }).subscribe({
      next: (job) => {
        this.router.navigate(['/jobs']);
      },
      error: (error) => {
        console.error('Error creating job:', error);
        alert('Failed to create job. Please try again.');
      },
    });
  }

  generateReport(): void {
    this.jobService.createJob({ jobType: 'GenerateTaskReport' }).subscribe({
      next: (job) => {
        this.router.navigate(['/jobs']);
      },
      error: (error) => {
        console.error('Error creating job:', error);
        alert('Failed to create job. Please try again.');
      },
    });
  }

  refreshData(): void {
    this.loadDashboardData();
  }
}
