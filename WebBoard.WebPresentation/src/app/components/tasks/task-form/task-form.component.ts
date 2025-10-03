import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  TaskDto,
  CreateTaskRequestDto,
  UpdateTaskRequestDto,
  TaskItemStatus,
} from '../../../models';
import { TaskService } from '../../../services';

@Component({
  selector: 'app-task-form',
  templateUrl: './task-form.component.html',
  styleUrls: ['./task-form.component.scss'],
})
export class TaskFormComponent implements OnInit {
  @Input() task: TaskDto | null = null;
  @Input() isEdit = false;
  @Output() save = new EventEmitter<TaskDto>();
  @Output() cancel = new EventEmitter<void>();

  taskForm!: FormGroup;
  saving = false;
  TaskItemStatus = TaskItemStatus;

  constructor(private fb: FormBuilder, private taskService: TaskService) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  private initializeForm(): void {
    this.taskForm = this.fb.group({
      title: [
        this.task?.title || '',
        [Validators.required, Validators.minLength(3)],
      ],
      description: [this.task?.description || '', [Validators.required]],
      status: [this.task?.status || TaskItemStatus.NotStarted],
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.taskForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.taskForm.valid) {
      this.saving = true;

      if (this.isEdit && this.task) {
        const updateRequest: UpdateTaskRequestDto = {
          title: this.taskForm.value.title,
          description: this.taskForm.value.description,
          status: this.taskForm.value.status,
        };

        this.taskService.updateTask(this.task.id, updateRequest).subscribe({
          next: (updatedTask) => {
            this.saving = false;
            this.save.emit(updatedTask);
          },
          error: (error) => {
            console.error('Error updating task:', error);
            this.saving = false;
            alert('Failed to update task. Please try again.');
          },
        });
      } else {
        const createRequest: CreateTaskRequestDto = {
          title: this.taskForm.value.title,
          description: this.taskForm.value.description,
        };

        this.taskService.createTask(createRequest).subscribe({
          next: (newTask) => {
            this.saving = false;
            this.save.emit(newTask);
          },
          error: (error) => {
            console.error('Error creating task:', error);
            this.saving = false;
            alert('Failed to create task. Please try again.');
          },
        });
      }
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
