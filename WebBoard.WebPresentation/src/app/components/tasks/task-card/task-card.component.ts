import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TaskDto, TaskItemStatus } from '../../../models';

@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.component.html',
  styleUrls: ['./task-card.component.scss'],
})
export class TaskCardComponent {
  @Input() task!: TaskDto;
  @Output() edit = new EventEmitter<TaskDto>();
  @Output() view = new EventEmitter<TaskDto>();
  @Output() delete = new EventEmitter<TaskDto>();

  TaskItemStatus = TaskItemStatus;

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

  getStatusIcon(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.Pending:
        return 'fas fa-clock';
      case TaskItemStatus.InProgress:
        return 'fas fa-spinner';
      case TaskItemStatus.Completed:
        return 'fas fa-check-circle';
      default:
        return 'fas fa-clock';
    }
  }

  onEdit(): void {
    this.edit.emit(this.task);
  }

  onView(): void {
    this.view.emit(this.task);
  }

  onDelete(): void {
    this.delete.emit(this.task);
  }
}
