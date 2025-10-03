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

  getStatusText(status: TaskItemStatus): string {
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
