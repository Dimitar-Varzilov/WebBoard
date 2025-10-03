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
    this.edit.emit(this.task);
  }

  onView(): void {
    this.view.emit(this.task);
  }

  onDelete(): void {
    this.delete.emit(this.task);
  }
}
