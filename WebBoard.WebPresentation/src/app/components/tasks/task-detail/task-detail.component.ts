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

  onEdit(): void {
    if (!this.task) {
      return;
    }
    this.edit.emit(this.task);
  }

  onDelete(): void {
    if (!this.task) {
      return;
    }
    this.delete.emit(this.task);
  }

  onClose(): void {
    this.close.emit();
  }
}
