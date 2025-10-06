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
    if (this.isEdit && this.task) {
      // Edit mode - include status field
      this.taskForm = this.fb.group({
        title: [
          this.task.title,
          [Validators.required, Validators.minLength(3)],
        ],
        description: [
          this.task.description, 
          [Validators.required, Validators.minLength(3)]
        ],
        status: [this.task.status, Validators.required],
      });
    } else {
      // Create mode - no status field (will be set to Pending automatically)
      this.taskForm = this.fb.group({
        title: [
          '',
          [Validators.required, Validators.minLength(3)],
        ],
        description: [
          '', 
          [Validators.required, Validators.minLength(3)]
        ],
      });
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.taskForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.taskForm.get(fieldName);
    if (field?.errors && field?.touched) {
      if (field.errors['required']) {
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
      }
      if (field.errors['minlength']) {
        const requiredLength = field.errors['minlength'].requiredLength;
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be at least ${requiredLength} characters`;
      }
    }
    return '';
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
            this.handleError(error, 'update');
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
            this.handleError(error, 'create');
          },
        });
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }

  private markFormGroupTouched(): void {
    Object.keys(this.taskForm.controls).forEach(key => {
      const control = this.taskForm.get(key);
      control?.markAsTouched();
    });
  }

  private handleError(error: any, operation: string): void {
    if (error.status === 400 && error.error) {
      const errorMessage = this.extractValidationErrors(error.error);
      alert(`Failed to ${operation} task: ${errorMessage}`);
    } else {
      alert(`Failed to ${operation} task. Please try again.`);
    }
  }

  private extractValidationErrors(errorResponse: any): string {
    if (typeof errorResponse === 'string') {
      return errorResponse;
    }
    
    if (errorResponse.message) {
      return errorResponse.message;
    }
    
    if (errorResponse.errors) {
      const errors: string[] = [];
      Object.keys(errorResponse.errors).forEach(key => {
        const fieldErrors = errorResponse.errors[key];
        if (Array.isArray(fieldErrors)) {
          errors.push(...fieldErrors);
        } else {
          errors.push(fieldErrors);
        }
      });
      return errors.join(', ');
    }
    
    return 'Unknown validation error';
  }
}
