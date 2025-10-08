import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TaskDto, TaskItemStatus } from '../../../models';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.scss'],
})
export class TaskDetailComponent {
  @Input() task: TaskDto | null = null;
  @Output() edit = new EventEmitter<TaskDto>();
  @Output() delete = new EventEmitter<TaskDto>();
  @Output() close = new EventEmitter<void>();

  getStatusClass(status: TaskItemStatus): string {
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

  getStatusText(status: TaskItemStatus): string {
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

  /**
   * Check if task is completed (read-only)
   */
  isTaskCompleted(): boolean {
    return this.task?.status === TaskItemStatus.Completed;
  }

  /**
   * Check if task can be edited
   */
  canEdit(): boolean {
    if (!this.task) return false;
    return this.task.status !== TaskItemStatus.Completed;
  }

  /**
   * Check if task can be deleted
   */
  canDelete(): boolean {
    if (!this.task) return false;
    return this.task.status !== TaskItemStatus.Completed;
  }

  onEdit(): void {
    if (!this.task || !this.canEdit()) {
      return;
    }
    this.edit.emit(this.task);
  }

  onDelete(): void {
    if (!this.task || !this.canDelete()) {
      return;
    }
    this.delete.emit(this.task);
  }

  onClose(): void {
    this.close.emit();
  }
}
