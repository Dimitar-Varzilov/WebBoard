import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TaskDto, TaskItemStatus } from '../../../models';
import { TaskService } from '../../../services';
import { ROUTES, TASK_STATUS_OPTIONS } from '../../../constants';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
})
export class TaskListComponent implements OnInit {
  tasks: TaskDto[] = [];
  filteredTasks: TaskDto[] = [];
  loading = false;
  searchText = '';
  statusFilter = '';
  taskStatusOptions = TASK_STATUS_OPTIONS;

  // Modal states
  showTaskForm = false;
  showTaskDetail = false;
  selectedTask: TaskDto | null = null;
  isEditMode = false;

  constructor(private taskService: TaskService, private router: Router) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading = true;
    this.taskService.getAllTasks().subscribe({
      next: (tasks) => {
        this.tasks = tasks;
        this.filterTasks();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading tasks:', error);
        this.loading = false;
      },
    });
  }

  refreshTasks(): void {
    this.loadTasks();
  }

  filterTasks(): void {
    this.filteredTasks = this.tasks.filter((task) => {
      const matchesSearch =
        !this.searchText ||
        task.title.toLowerCase().includes(this.searchText.toLowerCase()) ||
        task.description.toLowerCase().includes(this.searchText.toLowerCase());

      const matchesStatus =
        !this.statusFilter || task.status.toString() === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  clearFilters(): void {
    this.searchText = '';
    this.statusFilter = '';
    this.filterTasks();
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
          this.loadTasks();
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
    this.loadTasks();
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
